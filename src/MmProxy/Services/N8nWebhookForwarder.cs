using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MmProxy.Configuration;
using MmProxy.Models;

namespace MmProxy.Services;

public class N8nWebhookForwarder(
    N8nOptions n8nOptions,
    IHttpClientFactory httpClientFactory,
    ILogger<N8nWebhookForwarder> logger)
{
    private readonly ConcurrentDictionary<string, MattermostUser> _userCache = new();

    public async Task ForwardEventAsync(MattermostPost post, string channelType)
    {
        try
        {
            var user = await GetUserAsync(post.UserId);
            if (user == null)
            {
                logger.LogWarning("Unable to get user info for user_id: {UserId}, skipping event", post.UserId);
                return;
            }

            var eventType = channelType == "D" ? "dm" : "thread_post";
            
            var files = post.Metadata?.Files
                ?.Select(f => new N8nFile(f.Id, f.Name))
                .ToArray() ?? [];

            var payload = new N8nWebhookPayload(
                Type: eventType,
                EventTs: post.CreateAt,
                PostId: post.Id,
                RootId: post.RootId,
                ChannelId: post.ChannelId,
                ChannelType: channelType,
                User: new N8nUser(user.Id, user.Username),
                Text: post.Message,
                Files: files,
                Raw: post
            );

            await SendToN8nAsync(payload);
            
            logger.LogInformation("Forwarded {Type} event to n8n: post_id={PostId}", eventType, post.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to forward event to n8n: post_id={PostId}", post.Id);
        }
    }

    private async Task<MattermostUser?> GetUserAsync(string userId)
    {
        if (_userCache.TryGetValue(userId, out var cachedUser))
            return cachedUser;

        try
        {
            var client = httpClientFactory.CreateClient("MattermostApi");
            var requestUrl = $"users/{userId}";
            logger.LogInformation("Requesting user info: {BaseUrl}{Path}", client.BaseAddress, requestUrl);
            var response = await client.GetAsync(requestUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogWarning("Failed to get user info: {StatusCode}, Body: {Body}", 
                    response.StatusCode, errorBody.Length > 500 ? errorBody[..500] : errorBody);
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            
            // Check if response is HTML instead of JSON
            if (responseBody.TrimStart().StartsWith('<'))
            {
                logger.LogError("Received HTML instead of JSON from Mattermost API. Response: {Response}", 
                    responseBody.Length > 500 ? responseBody[..500] : responseBody);
                return null;
            }

            var user = JsonSerializer.Deserialize<MattermostUser>(responseBody);
            if (user != null)
            {
                _userCache.TryAdd(userId, user);
            }
            
            return user;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user info for user_id: {UserId}", userId);
            return null;
        }
    }

    private async Task SendToN8nAsync(N8nWebhookPayload payload)
    {
        var client = httpClientFactory.CreateClient("N8nWebhook");
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var request = new HttpRequestMessage(HttpMethod.Post, n8nOptions.InboundWebhookUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        
        request.Headers.Add("X-Webhook-Secret", n8nOptions.WebhookSecret);

        var response = await client.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            logger.LogError("n8n webhook returned {StatusCode}: {Body}", response.StatusCode, errorBody);
        }
    }
}
