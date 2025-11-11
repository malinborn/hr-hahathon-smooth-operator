using System.Net.WebSockets;
using System.Text.Json;
using MmProxy.Configuration;
using MmProxy.Models;
using Websocket.Client;

namespace MmProxy.Services;

public class MattermostWebSocketClient(
    MattermostOptions options,
    ILogger<MattermostWebSocketClient> logger) : IHostedService, IAsyncDisposable
{
    private WebsocketClient? _client;
    private CancellationTokenSource? _reconnectCts;
    private int _reconnectDelaySeconds = 1;
    private const int MaxReconnectDelaySeconds = 30;

    public event Func<MattermostPost, Task>? OnPostReceived;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting Mattermost WebSocket client");
        await ConnectAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping Mattermost WebSocket client");
        _reconnectCts?.Cancel();
        
        if (_client != null)
        {
            await _client.Stop(WebSocketCloseStatus.NormalClosure, "Service stopping");
        }
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            var wsUrl = $"{options.WsUrl}?connection_id=&sequence_number=0";
            var factory = new Func<ClientWebSocket>(() =>
            {
                var ws = new ClientWebSocket();
                ws.Options.SetRequestHeader("Authorization", $"Bearer {options.BotToken}");
                return ws;
            });

            _client = new WebsocketClient(new Uri(wsUrl), factory);
            
            _client.ReconnectTimeout = null; // Disable built-in reconnection, we'll handle it manually
            _client.ErrorReconnectTimeout = null;
            
            _client.MessageReceived.Subscribe(HandleMessage);
            _client.DisconnectionHappened.Subscribe(info =>
            {
                logger.LogWarning("WebSocket disconnected: {Type}, {CloseStatus}", info.Type, info.CloseStatus);
                _ = ReconnectAsync(CancellationToken.None);
            });

            await _client.StartOrFail();
            logger.LogInformation("WebSocket connected successfully");
            _reconnectDelaySeconds = 1; // Reset backoff on successful connection
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to WebSocket");
            _ = ReconnectAsync(cancellationToken);
        }
    }

    private async Task ReconnectAsync(CancellationToken cancellationToken)
    {
        _reconnectCts?.Cancel();
        _reconnectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        logger.LogInformation("Attempting to reconnect in {Delay} seconds", _reconnectDelaySeconds);
        
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(_reconnectDelaySeconds), _reconnectCts.Token);
            
            // Exponential backoff
            _reconnectDelaySeconds = Math.Min(_reconnectDelaySeconds * 2, MaxReconnectDelaySeconds);
            
            await ConnectAsync(_reconnectCts.Token);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Reconnection cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Reconnection attempt failed");
        }
    }

    private void HandleMessage(ResponseMessage message)
    {
        try
        {
            if (message.Text == null)
                return;

            var mmEvent = JsonSerializer.Deserialize<MattermostEvent>(message.Text);
            
            if (mmEvent?.Event != "posted" || mmEvent.Data?.Post == null)
                return;

            var post = JsonSerializer.Deserialize<MattermostPost>(mmEvent.Data.Post);
            
            if (post == null)
                return;

            // Filter own messages if BotUserId is configured
            if (!string.IsNullOrEmpty(options.BotUserId) && post.UserId == options.BotUserId)
                return;

            // Filter: only DM (channel_type = "D") or posts with root_id (threads)
            var isDm = mmEvent.Data.ChannelType == "D";
            var isThread = !string.IsNullOrEmpty(post.RootId);

            if (!isDm && !isThread)
                return;

            logger.LogInformation("Received {Type} post: {Id} in channel {ChannelId}", 
                isDm ? "DM" : "thread", post.Id, post.ChannelId);

            OnPostReceived?.Invoke(post);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse WebSocket message: {Message}", message.Text);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling WebSocket message");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _reconnectCts?.Cancel();
        _reconnectCts?.Dispose();
        _client?.Dispose();
        await Task.CompletedTask;
    }
}
