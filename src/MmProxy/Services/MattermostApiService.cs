using System.Text.Json;
using MmProxy.Configuration;
using MmProxy.Models;

namespace MmProxy.Services;

public class MattermostApiService(IHttpClientFactory httpClientFactory, ILogger<MattermostApiService> logger)
{
    readonly HttpClient _httpClient = httpClientFactory.CreateClient("MattermostApi");

    public async Task<MattermostPost> CreatePostAsync(CreatePostRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating post in channel {ChannelId}, root_id: {RootId}", request.ChannelId, request.RootId);

        var response = await _httpClient.PostAsJsonAsync("posts", request, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to create post: {StatusCode} - {Error}", response.StatusCode, error);
            throw new HttpRequestException($"Failed to create post: {response.StatusCode}");
        }

        var post = await response.Content.ReadFromJsonAsync<MattermostPost>(cancellationToken);
        logger.LogInformation("Post created successfully: {PostId}", post?.Id);
        
        return post ?? throw new InvalidOperationException("Failed to deserialize post response");
    }

    public async Task<(MattermostPost Root, List<MattermostPost> Replies)> GetThreadAsync(
        string rootId, 
        int limit = 50, 
        string order = "asc",
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting thread for root_id: {RootId}, limit: {Limit}, order: {Order}", rootId, limit, order);

        // First, get the root post to determine the channel_id
        var rootPost = await GetPostAsync(rootId, cancellationToken);
        
        // Then get the thread replies
        var response = await _httpClient.GetAsync($"posts/{rootId}/thread", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to get thread: {StatusCode} - {Error}", response.StatusCode, error);
            throw new HttpRequestException($"Failed to get thread: {response.StatusCode}");
        }

        var postsResponse = await response.Content.ReadFromJsonAsync<PostsResponse>(cancellationToken);
        if (postsResponse == null)
            throw new InvalidOperationException("Failed to deserialize thread response");

        // Extract replies (excluding root post)
        var replies = postsResponse.Order
            .Where(id => id != rootId)
            .Select(id => postsResponse.Posts[id])
            .ToList();

        // Sort based on order parameter
        replies = order.ToLowerInvariant() == "desc" 
            ? replies.OrderByDescending(p => p.CreateAt).ToList()
            : replies.OrderBy(p => p.CreateAt).ToList();

        // Apply limit
        if (limit > 0 && replies.Count > limit)
            replies = replies.Take(limit).ToList();

        logger.LogInformation("Thread retrieved: {RepliesCount} replies", replies.Count);
        
        return (rootPost, replies);
    }

    public async Task<MattermostPost> GetPostAsync(string postId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting post: {PostId}", postId);

        var response = await _httpClient.GetAsync($"posts/{postId}", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to get post: {StatusCode} - {Error}", response.StatusCode, error);
            throw new HttpRequestException($"Failed to get post: {response.StatusCode}");
        }

        var post = await response.Content.ReadFromJsonAsync<MattermostPost>(cancellationToken);
        return post ?? throw new InvalidOperationException("Failed to deserialize post response");
    }

    public async Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting user by username: {Username}", username);

        var response = await _httpClient.GetAsync($"users/username/{username}", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger.LogWarning("User not found: {Username}", username);
                return null;
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to get user: {StatusCode} - {Error}", response.StatusCode, error);
            throw new HttpRequestException($"Failed to get user: {response.StatusCode}");
        }

        return await response.Content.ReadFromJsonAsync<User>(cancellationToken);
    }

    public async Task<Channel> CreateDirectChannelAsync(string userId1, string userId2, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Creating direct channel between {UserId1} and {UserId2}", userId1, userId2);

        var request = new CreateDirectChannelRequest([userId1, userId2]);
        var response = await _httpClient.PostAsJsonAsync("channels/direct", request, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to create direct channel: {StatusCode} - {Error}", response.StatusCode, error);
            throw new HttpRequestException($"Failed to create direct channel: {response.StatusCode}");
        }

        var channel = await response.Content.ReadFromJsonAsync<Channel>(cancellationToken);
        return channel ?? throw new InvalidOperationException("Failed to deserialize channel response");
    }
}
