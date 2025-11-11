using System.Text.Json.Serialization;

namespace MmProxy.Models;

public record MattermostEvent(
    [property: JsonPropertyName("event")] string Event,
    [property: JsonPropertyName("data")] MattermostEventData? Data,
    [property: JsonPropertyName("broadcast")] MattermostBroadcast? Broadcast,
    [property: JsonPropertyName("seq")] int Seq
);

public record MattermostEventData(
    [property: JsonPropertyName("post")] string? Post,
    [property: JsonPropertyName("channel_id")] string? ChannelId,
    [property: JsonPropertyName("channel_type")] string? ChannelType,
    [property: JsonPropertyName("sender_name")] string? SenderName,
    [property: JsonPropertyName("team_id")] string? TeamId
);

public record MattermostBroadcast(
    [property: JsonPropertyName("user_id")] string? UserId,
    [property: JsonPropertyName("channel_id")] string? ChannelId,
    [property: JsonPropertyName("team_id")] string? TeamId
);

public record MattermostPost(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("create_at")] long CreateAt,
    [property: JsonPropertyName("update_at")] long UpdateAt,
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("channel_id")] string ChannelId,
    [property: JsonPropertyName("root_id")] string? RootId,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("props")] Dictionary<string, object>? Props,
    [property: JsonPropertyName("hashtags")] string? Hashtags,
    [property: JsonPropertyName("pending_post_id")] string? PendingPostId
);
