using SkyBooker.BookingService.Entities;

namespace SkyBooker.BookingService.Repositories;

public interface IBookingRepository
{
    Task<IList<Booking>> FindByUserIdAsync(int userId);
    Task<Booking?> FindByPnrCodeAsync(string pnrCode);
    Task<IList<Booking>> FindByFlightIdAsync(int flightId);
    Task<IList<Booking>> FindByStatusAsync(string status);
    Task<Booking?> FindByBookingIdAsync(string bookingId);
    Task<int> CountByFlightIdAndStatusAsync(int flightId, string status);
    Task<IList<Booking>> FindByUserIdAndStatusAsync(int userId, string status);
    Task<bool> ExistsByPnrCodeAsync(string pnrCode);
    Task<Booking> CreateAsync(Booking booking);
    Task<Booking> UpdateAsync(Booking booking);
}
