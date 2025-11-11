using System.Text.Json.Serialization;

namespace MmProxy.Models;

public record MattermostUser(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("nickname")] string? Nickname,
    [property: JsonPropertyName("first_name")] string? FirstName,
    [property: JsonPropertyName("last_name")] string? LastName
);
