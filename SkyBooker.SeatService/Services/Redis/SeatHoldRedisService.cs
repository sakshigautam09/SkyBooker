using StackExchange.Redis;

namespace SkyBooker.SeatService.Services.Redis;

public class SeatHoldRedisService : ISeatHoldRedisService
{
    private readonly IDatabase _db;
    private readonly ILogger<SeatHoldRedisService> _logger;

    // Default hold TTL matches case study: 15 minutes
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(15);

    public SeatHoldRedisService(IConnectionMultiplexer redis, ILogger<SeatHoldRedisService> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    // Key pattern: seat:hold:{seatId}
    private static string HoldKey(int seatId) => $"seat:hold:{seatId}";

    public async Task HoldSeatAsync(int seatId, int userId, TimeSpan? ttl = null)
    {
        var expiry = ttl ?? DefaultTtl;
        await _db.StringSetAsync(HoldKey(seatId), userId.ToString(), expiry);
        _logger.LogInformation(
            "[Redis] Seat {SeatId} held by User {UserId} — expires in {Minutes} min",
            seatId, userId, expiry.TotalMinutes);
    }

    public async Task<bool> IsSeatHeldAsync(int seatId)
        => await _db.KeyExistsAsync(HoldKey(seatId));

    public async Task<int?> GetHoldingUserIdAsync(int seatId)
    {
        var value = await _db.StringGetAsync(HoldKey(seatId));
        if (value.IsNullOrEmpty) return null;
        return int.TryParse(value, out var userId) ? userId : null;
    }

    public async Task ReleaseSeatHoldAsync(int seatId)
    {
        await _db.KeyDeleteAsync(HoldKey(seatId));
        _logger.LogInformation("[Redis] Seat hold released: SeatId={SeatId}", seatId);
    }

    public async Task ExtendHoldAsync(int seatId, TimeSpan ttl)
    {
        await _db.KeyExpireAsync(HoldKey(seatId), ttl);
        _logger.LogInformation(
            "[Redis] Seat {SeatId} hold extended by {Minutes} min", seatId, ttl.TotalMinutes);
    }
}
