using MmProxy.Configuration;
using MmProxy.Endpoints;
using MmProxy.Middleware;
using MmProxy.Services;
using Polly;
using Polly.Extensions.Http;

// Load .env file if exists
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Configure port from environment
var servicePort = int.TryParse(builder.Configuration["SERVICE_PORT"] ?? builder.Configuration["Service:Port"], out var p) ? p : 8080;
builder.WebHost.UseUrls($"http://0.0.0.0:{servicePort}");

// Configure Mattermost options from environment variables and appsettings
var mattermostOptions = new MattermostOptions(
    WsUrl: builder.Configuration["MATTERMOST_WS_URL"] ?? builder.Configuration["Mattermost:WsUrl"] ?? "",
    ApiUrl: builder.Configuration["MATTERMOST_API_URL"] ?? builder.Configuration["Mattermost:ApiUrl"] ?? "",
    BotToken: builder.Configuration["MATTERMOST_BOT_TOKEN"] ?? builder.Configuration["Mattermost:BotToken"] ?? "",
    BotUserId: builder.Configuration["BOT_USER_ID"] ?? builder.Configuration["Mattermost:BotUserId"]
);

builder.Services.AddSingleton(mattermostOptions);

// Configure n8n options
var n8nOptions = new N8nOptions(
    InboundWebhookUrl: builder.Configuration["N8N_INBOUND_WEBHOOK_URL"] ?? builder.Configuration["N8n:InboundWebhookUrl"] ?? "",
    WebhookSecret: builder.Configuration["N8N_WEBHOOK_SECRET"] ?? builder.Configuration["N8n:WebhookSecret"] ?? ""
);

builder.Services.AddSingleton(n8nOptions);

// Configure service options
var serviceOptions = new ServiceOptions(
    ApiKey: builder.Configuration["SERVICE_API_KEY"] ?? builder.Configuration["Service:ApiKey"] ?? "",
    Port: int.TryParse(builder.Configuration["SERVICE_PORT"] ?? builder.Configuration["Service:Port"], out var port) ? port : 8080
);

builder.Services.AddSingleton(serviceOptions);

// Configure retry policy for HTTP clients
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

// Configure HttpClient for Mattermost API
builder.Services.AddHttpClient("MattermostApi", client =>
{
    // Ensure BaseAddress ends with /
    var baseUrl = mattermostOptions.ApiUrl.TrimEnd('/') + "/";
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Authorization = 
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", mattermostOptions.BotToken);
    client.Timeout = TimeSpan.FromSeconds(10);
})
.AddPolicyHandler(retryPolicy);

// Configure HttpClient for n8n webhook (no retries for webhooks)
builder.Services.AddHttpClient("N8nWebhook", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Add services
builder.Services.AddSingleton<N8nWebhookForwarder>();
builder.Services.AddSingleton<MattermostApiService>();

// Add WebSocket client as hosted service
builder.Services.AddSingleton<MattermostWebSocketClient>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MattermostWebSocketClient>());

// Add controllers and OpenAPI for future HTTP API endpoints
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Subscribe to WebSocket events and forward to n8n
var wsClient = app.Services.GetRequiredService<MattermostWebSocketClient>();
var forwarder = app.Services.GetRequiredService<N8nWebhookForwarder>();
wsClient.OnPostReceived += async (post, channelType) =>
{
    await forwarder.ForwardEventAsync(post, channelType);
};

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Add API key authentication middleware
app.UseMiddleware<ApiKeyAuthMiddleware>();

app.MapControllers();

// Map API endpoints
app.MapApiEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("HealthCheck");

app.Run();
