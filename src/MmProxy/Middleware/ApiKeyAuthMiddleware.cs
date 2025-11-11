using MmProxy.Configuration;

namespace MmProxy.Middleware;

public class ApiKeyAuthMiddleware(RequestDelegate next, ServiceOptions serviceOptions, ILogger<ApiKeyAuthMiddleware> logger)
{
    const string ApiKeyHeader = "X-API-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for health endpoint
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await next(context);
            return;
        }

        // Check for API key header
        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var extractedApiKey))
        {
            logger.LogWarning("Missing {HeaderName} header from {RemoteIp}", ApiKeyHeader, context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { ok = false, error = "Missing X-API-Key header" });
            return;
        }

        // Validate API key
        if (!string.Equals(extractedApiKey, serviceOptions.ApiKey, StringComparison.Ordinal))
        {
            logger.LogWarning("Invalid {HeaderName} from {RemoteIp}", ApiKeyHeader, context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { ok = false, error = "Invalid X-API-Key" });
            return;
        }

        await next(context);
    }
}
