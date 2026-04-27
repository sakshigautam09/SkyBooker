using StackExchange.Redis;

namespace SkyBooker.AuthService.Services.Redis;

public class RedisTokenService : IRedisTokenService
{
    private readonly IDatabase _db;

    // Key patterns:
    //   blacklist:{jti}               → blacklisted access token
    //   refresh:{userId}:{token}      → valid refresh token marker

    public RedisTokenService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    // ── Access Token Blacklist ────────────────────────────────────────────────

    public async Task BlacklistAccessTokenAsync(string token, TimeSpan expiry)
    {
        // Use a hash of the token as the key to avoid giant keys
        var key = $"blacklist:{ComputeHash(token)}";
        await _db.StringSetAsync(key, "1", expiry);
    }

    public async Task<bool> IsAccessTokenBlacklistedAsync(string token)
    {
        var key = $"blacklist:{ComputeHash(token)}";
        return await _db.KeyExistsAsync(key);
    }

    // ── Refresh Token Store ───────────────────────────────────────────────────

    public async Task StoreRefreshTokenAsync(int userId, string refreshToken, TimeSpan expiry)
    {
        var key = $"refresh:{userId}:{ComputeHash(refreshToken)}";
        await _db.StringSetAsync(key, "1", expiry);
    }

    public async Task<bool> IsRefreshTokenValidAsync(int userId, string refreshToken)
    {
        var key = $"refresh:{userId}:{ComputeHash(refreshToken)}";
        return await _db.KeyExistsAsync(key);
    }

    public async Task RevokeRefreshTokenAsync(int userId, string refreshToken)
    {
        var key = $"refresh:{userId}:{ComputeHash(refreshToken)}";
        await _db.KeyDeleteAsync(key);
    }

    public async Task RevokeAllRefreshTokensAsync(int userId)
    {
        // Scan for all refresh keys belonging to this user and delete them
        var server = _db.Multiplexer.GetServer(_db.Multiplexer.GetEndPoints().First());
        var pattern = $"refresh:{userId}:*";

        await foreach (var key in server.KeysAsync(pattern: pattern))
        {
            await _db.KeyDeleteAsync(key);
        }
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static string ComputeHash(string input)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16]; // first 16 hex chars = unique enough
    }
}
