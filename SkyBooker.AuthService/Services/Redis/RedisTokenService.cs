using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace SkyBooker.AuthService.Services.Redis;

public class RedisTokenService : IRedisTokenService
{
    private readonly IDatabase? _db;

    // Use IServiceProvider so IConnectionMultiplexer is truly optional in DI
    public RedisTokenService(IServiceProvider serviceProvider)
    {
        try
        {
            var multiplexer = serviceProvider.GetService<IConnectionMultiplexer>();
            if (multiplexer != null && multiplexer.IsConnected)
                _db = multiplexer.GetDatabase();
        }
        catch
        {
            _db = null;
        }
    }

    public async Task BlacklistAccessTokenAsync(string token, TimeSpan expiry)
    {
        if (_db is null) return;
        await _db.StringSetAsync($"blacklist:{Hash(token)}", "1", expiry);
    }

    public async Task<bool> IsAccessTokenBlacklistedAsync(string token)
    {
        if (_db is null) return false;
        return await _db.KeyExistsAsync($"blacklist:{Hash(token)}");
    }

    public async Task StoreRefreshTokenAsync(int userId, string refreshToken, TimeSpan expiry)
    {
        if (_db is null) return;
        await _db.StringSetAsync($"refresh:{userId}:{Hash(refreshToken)}", "1", expiry);
    }

    public async Task<bool> IsRefreshTokenValidAsync(int userId, string refreshToken)
    {
        if (_db is null) return true;
        return await _db.KeyExistsAsync($"refresh:{userId}:{Hash(refreshToken)}");
    }

    public async Task RevokeRefreshTokenAsync(int userId, string refreshToken)
    {
        if (_db is null) return;
        await _db.KeyDeleteAsync($"refresh:{userId}:{Hash(refreshToken)}");
    }

    public async Task RevokeAllRefreshTokensAsync(int userId)
    {
        if (_db is null) return;
        var server = _db.Multiplexer.GetServer(_db.Multiplexer.GetEndPoints().First());
        await foreach (var key in server.KeysAsync(pattern: $"refresh:{userId}:*"))
            await _db.KeyDeleteAsync(key);
    }

    private static string Hash(string input)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16];
    }
}