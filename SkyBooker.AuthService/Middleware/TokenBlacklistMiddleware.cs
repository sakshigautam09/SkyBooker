using SkyBooker.AuthService.Services.Redis;

namespace SkyBooker.AuthService.Middleware;

/// <summary>
/// Middleware that checks every incoming request's JWT against the Redis blacklist.
/// If a user has logged out, their token is blacklisted in Redis and all subsequent
/// requests with that token are rejected with 401 — even if the JWT signature is valid.
/// </summary>
public class TokenBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public TokenBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRedisTokenService redisTokenService)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (authHeader != null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();

            if (await redisTokenService.IsAccessTokenBlacklistedAsync(token))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Token has been revoked. Please login again."
                });
                return;
            }
        }

        await _next(context);
    }
}
