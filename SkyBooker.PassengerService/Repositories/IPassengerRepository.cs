using SkyBooker.PassengerService.Entities;

namespace SkyBooker.PassengerService.Repositories;

public interface IPassengerRepository
{
    Task<IList<PassengerInfo>> FindByBookingIdAsync(string bookingId);
    Task<PassengerInfo?> FindByPassengerIdAsync(int passengerId);
    Task<PassengerInfo?> FindByPassportNumberAsync(string passportNumber);
    Task<PassengerInfo?> FindByTicketNumberAsync(string ticketNumber);
    Task<PassengerInfo?> FindBySeatIdAsync(int seatId);
    Task<int> CountByBookingIdAsync(string bookingId);
    Task DeleteByBookingIdAsync(string bookingId);
    Task<bool> ExistsByTicketNumberAsync(string ticketNumber);
    Task<PassengerInfo> AddAsync(PassengerInfo passenger);
    Task<PassengerInfo> UpdateAsync(PassengerInfo passenger);
    Task DeleteAsync(int passengerId);
}
