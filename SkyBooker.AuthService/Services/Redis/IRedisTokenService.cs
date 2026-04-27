namespace SkyBooker.AuthService.Services.Redis;

public interface IRedisTokenService
{
    /// <summary>Blacklist a JWT access token until it naturally expires.</summary>
    Task BlacklistAccessTokenAsync(string token, TimeSpan expiry);

    /// <summary>Check if a JWT access token has been blacklisted (i.e. user logged out).</summary>
    Task<bool> IsAccessTokenBlacklistedAsync(string token);

    /// <summary>Store a refresh token in Redis (backup / fast lookup).</summary>
    Task StoreRefreshTokenAsync(int userId, string refreshToken, TimeSpan expiry);

    /// <summary>Check if a refresh token exists and is valid in Redis.</summary>
    Task<bool> IsRefreshTokenValidAsync(int userId, string refreshToken);

    /// <summary>Revoke a refresh token from Redis.</summary>
    Task RevokeRefreshTokenAsync(int userId, string refreshToken);

    /// <summary>Revoke ALL refresh tokens for a user (e.g. on deactivate).</summary>
    Task RevokeAllRefreshTokensAsync(int userId);
}
