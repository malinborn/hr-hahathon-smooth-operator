using System.Text.Json.Serialization;

namespace MmProxy.Models;

// POST /answer request models
public record AnswerRequest(
    [property: JsonPropertyName("mode")] string? Mode,
    [property: JsonPropertyName("channel_id")] string? ChannelId,
    [property: JsonPropertyName("root_id")] string? RootId,
    [property: JsonPropertyName("user_id")] string? UserId,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("props")] Dictionary<string, object>? Props,
    [property: JsonPropertyName("file_ids")] List<string>? FileIds
);

public record AnswerResponse(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("post_id")] string PostId,
    [property: JsonPropertyName("channel_id")] string ChannelId,
    [property: JsonPropertyName("root_id")] string? RootId
);

// POST /get_thread request/response models
public record GetThreadRequest(
    [property: JsonPropertyName("root_id")] string RootId,
    [property: JsonPropertyName("limit")] int? Limit,
    [property: JsonPropertyName("order")] string? Order
);

public record GetThreadResponse(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("root")] ThreadPost Root,
    [property: JsonPropertyName("replies")] List<ThreadPost> Replies
);

public record ThreadPost(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("channel_id")] string ChannelId,
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("create_at")] long CreateAt,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("root_id")] string? RootId,
    [property: JsonPropertyName("props")] Dictionary<string, object>? Props,
    [property: JsonPropertyName("files")] List<ThreadFile> Files
);

public record ThreadFile(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name
);

// Mattermost API models
public record CreatePostRequest(
    [property: JsonPropertyName("channel_id")] string ChannelId,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("root_id")] string? RootId,
    [property: JsonPropertyName("props")] Dictionary<string, object>? Props,
    [property: JsonPropertyName("file_ids")] List<string>? FileIds
);

public record CreateDirectChannelRequest(
    [property: JsonPropertyName("user_ids")] List<string> UserIds
);

public record Channel(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("name")] string Name
);

public record User(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("username")] string Username
);

public record PostsResponse(
    [property: JsonPropertyName("order")] List<string> Order,
    [property: JsonPropertyName("posts")] Dictionary<string, MattermostPost> Posts
);
