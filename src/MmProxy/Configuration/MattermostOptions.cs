namespace MmProxy.Configuration;

public record MattermostOptions(
    string WsUrl,
    string ApiUrl,
    string BotToken,
    string? BotUserId
);
