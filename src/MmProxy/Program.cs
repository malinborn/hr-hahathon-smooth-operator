using MmProxy.Configuration;
using MmProxy.Services;

// Load .env file if exists
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Configure Mattermost options from environment variables and appsettings
var mattermostOptions = new MattermostOptions(
    WsUrl: builder.Configuration["MATTERMOST_WS_URL"] ?? builder.Configuration["Mattermost:WsUrl"] ?? "",
    ApiUrl: builder.Configuration["MATTERMOST_API_URL"] ?? builder.Configuration["Mattermost:ApiUrl"] ?? "",
    BotToken: builder.Configuration["MATTERMOST_BOT_TOKEN"] ?? builder.Configuration["Mattermost:BotToken"] ?? "",
    BotUserId: builder.Configuration["BOT_USER_ID"] ?? builder.Configuration["Mattermost:BotUserId"]
);

builder.Services.AddSingleton(mattermostOptions);

// Add WebSocket client as hosted service
builder.Services.AddSingleton<MattermostWebSocketClient>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MattermostWebSocketClient>());

// Add controllers and OpenAPI for future HTTP API endpoints
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("HealthCheck");

app.Run();
