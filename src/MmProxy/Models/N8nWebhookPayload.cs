using System.Text.Json.Serialization;

namespace MmProxy.Models;

public record N8nWebhookPayload(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("event_ts")] long EventTs,
    [property: JsonPropertyName("post_id")] string PostId,
    [property: JsonPropertyName("root_id")] string? RootId,
    [property: JsonPropertyName("channel_id")] string ChannelId,
    [property: JsonPropertyName("channel_type")] string ChannelType,
    [property: JsonPropertyName("user")] N8nUser User,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("files")] N8nFile[] Files,
    [property: JsonPropertyName("raw")] object? Raw
);

public record N8nUser(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("username")] string Username
);

public record N8nFile(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name
);
