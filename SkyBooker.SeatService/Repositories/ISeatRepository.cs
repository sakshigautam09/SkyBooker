using SkyBooker.SeatService.Entities;

namespace SkyBooker.SeatService.Repositories;

public interface ISeatRepository
{
    Task<IList<Seat>> FindByFlightIdAsync(int flightId);
    Task<IList<Seat>> FindByFlightIdAndSeatClassAsync(int flightId, string seatClass);
    Task<Seat?> FindBySeatIdAsync(int seatId);
    Task<IList<Seat>> FindAvailableByFlightIdAsync(int flightId);
    Task<Seat?> FindByFlightIdAndSeatNumberAsync(int flightId, string seatNumber);
    Task<int> CountAvailableByClassAsync(int flightId, string seatClass);
    Task DeleteByFlightIdAsync(int flightId);
    Task<IList<Seat>> AddRangeAsync(IList<Seat> seats);
    Task<Seat> UpdateAsync(Seat seat);
    Task<IList<Seat>> FindHeldSeatsExpiredAsync(DateTime expiryThreshold);
}
