namespace SkyBooker.SeatService.Services.Redis;

public interface ISeatHoldRedisService
{
    /// <summary>
    /// Store a seat hold in Redis with 15-min TTL.
    /// Key: seat:hold:{seatId}  Value: userId
    /// </summary>
    Task HoldSeatAsync(int seatId, int userId, TimeSpan? ttl = null);

    /// <summary>Check if a seat is currently held in Redis.</summary>
    Task<bool> IsSeatHeldAsync(int seatId);

    /// <summary>Get userId who holds this seat. Returns null if not held.</summary>
    Task<int?> GetHoldingUserIdAsync(int seatId);

    /// <summary>Remove seat hold from Redis (release or confirm).</summary>
    Task ReleaseSeatHoldAsync(int seatId);

    /// <summary>Extend TTL on an existing hold (optional future use).</summary>
    Task ExtendHoldAsync(int seatId, TimeSpan ttl);
}
