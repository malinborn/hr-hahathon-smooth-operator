using Microsoft.AspNetCore.Mvc;
using MmProxy.Configuration;
using MmProxy.Models;
using MmProxy.Services;

namespace MmProxy.Endpoints;

public static class ApiEndpoints
{
    public static void MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/answer", AnswerAsync)
            .WithName("Answer")
            .WithOpenApi();

        app.MapPost("/get_thread", GetThreadAsync)
            .WithName("GetThread")
            .WithOpenApi();
    }

    static async Task<IResult> AnswerAsync(
        [FromBody] AnswerRequest request,
        [FromServices] MattermostApiService mmApiService,
        [FromServices] MattermostOptions mmOptions,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Text))
                return Results.BadRequest(new { ok = false, error = "Text is required" });

            // Determine mode: thread or dm
            var isThreadMode = !string.IsNullOrWhiteSpace(request.ChannelId) && !string.IsNullOrWhiteSpace(request.RootId);
            var isDmMode = !string.IsNullOrWhiteSpace(request.UserId) || !string.IsNullOrWhiteSpace(request.Username);

            if (!isThreadMode && !isDmMode)
                return Results.BadRequest(new { ok = false, error = "Either (channel_id + root_id) or (user_id/username) must be provided" });

            string channelId;
            string? rootId = null;

            if (isThreadMode)
            {
                // Thread mode
                channelId = request.ChannelId!;
                rootId = request.RootId;
            }
            else
            {
                // DM mode
                var targetUserId = request.UserId;
                
                // If username is provided, resolve to user_id
                if (string.IsNullOrWhiteSpace(targetUserId) && !string.IsNullOrWhiteSpace(request.Username))
                {
                    var user = await mmApiService.GetUserByUsernameAsync(request.Username, cancellationToken);
                    if (user == null)
                        return Results.NotFound(new { ok = false, error = $"User not found: {request.Username}" });
                    
                    targetUserId = user.Id;
                }

                if (string.IsNullOrWhiteSpace(targetUserId))
                    return Results.BadRequest(new { ok = false, error = "user_id or username is required for DM mode" });

                // Get bot user ID
                if (string.IsNullOrWhiteSpace(mmOptions.BotUserId))
                    return Results.BadRequest(new { ok = false, error = "BOT_USER_ID is not configured" });

                // Create or get DM channel
                var channel = await mmApiService.CreateDirectChannelAsync(mmOptions.BotUserId, targetUserId, cancellationToken);
                channelId = channel.Id;
            }

            // Create post
            var createPostRequest = new CreatePostRequest(
                ChannelId: channelId,
                Message: request.Text,
                RootId: rootId,
                Props: request.Props,
                FileIds: request.FileIds
            );

            var post = await mmApiService.CreatePostAsync(createPostRequest, cancellationToken);

            return Results.Ok(new AnswerResponse(
                Ok: true,
                PostId: post.Id,
                ChannelId: post.ChannelId,
                RootId: post.RootId
            ));
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error while creating post");
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway,
                title: "Mattermost API Error"
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while creating post");
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error"
            );
        }
    }

    static async Task<IResult> GetThreadAsync(
        [FromBody] GetThreadRequest request,
        [FromServices] MattermostApiService mmApiService,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.RootId))
                return Results.BadRequest(new { ok = false, error = "root_id is required" });

            var limit = request.Limit ?? 50;
            var order = request.Order ?? "asc";

            // Validate order
            if (order != "asc" && order != "desc")
                return Results.BadRequest(new { ok = false, error = "order must be 'asc' or 'desc'" });

            // Get thread
            var (rootPost, replies) = await mmApiService.GetThreadAsync(request.RootId, limit, order, cancellationToken);

            // Map to response format
            var root = MapToThreadPost(rootPost);
            var repliesList = replies.Select(MapToThreadPost).ToList();

            return Results.Ok(new GetThreadResponse(
                Ok: true,
                Root: root,
                Replies: repliesList
            ));
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error while getting thread");
            
            if (ex.Message.Contains("404"))
                return Results.NotFound(new { ok = false, error = "Thread not found" });

            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway,
                title: "Mattermost API Error"
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while getting thread");
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error"
            );
        }
    }

    static ThreadPost MapToThreadPost(MattermostPost post) => new(
        Id: post.Id,
        ChannelId: post.ChannelId,
        UserId: post.UserId,
        CreateAt: post.CreateAt,
        Message: post.Message,
        RootId: post.RootId,
        Props: post.Props ?? [],
        Files: post.Metadata?.Files?.Select(f => new ThreadFile(f.Id, f.Name)).ToList() ?? []
    );
}
