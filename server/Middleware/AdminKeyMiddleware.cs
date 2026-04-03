using CyberServer.Services;

namespace CyberServer.Middleware;

/// <summary>
/// Validates the "X-Admin-Key" header for admin endpoints.
/// Set AdminApiKey in appsettings.json. Leave empty to disable auth (not recommended for production).
/// </summary>
public class AdminKeyMiddleware(RequestDelegate next, IConfiguration config)
{
    private readonly string _adminKey = config["AdminApiKey"] ?? string.Empty;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/admin"))
        {
            if (string.IsNullOrWhiteSpace(_adminKey))
            {
                // No key configured → allow (dev mode)
                await next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue("X-Admin-Key", out var key) || key != _adminKey)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "Unauthorized. Provide X-Admin-Key header." });
                return;
            }
        }

        await next(context);
    }
}
