using Microsoft.EntityFrameworkCore;
using SkyBooker.PassengerService.Context;
using SkyBooker.PassengerService.Entities;

namespace SkyBooker.PassengerService.Repositories;

public class PassengerRepository : IPassengerRepository
{
    private readonly PassengerDbContext _context;

    public PassengerRepository(PassengerDbContext context)
    {
        _context = context;
    }

    public async Task<IList<PassengerInfo>> FindByBookingIdAsync(string bookingId)
        => await _context.Passengers
            .Where(p => p.BookingId == bookingId)
            .OrderBy(p => p.PassengerId)
            .ToListAsync();

    public async Task<PassengerInfo?> FindByPassengerIdAsync(int passengerId)
        => await _context.Passengers.FindAsync(passengerId);

    public async Task<PassengerInfo?> FindByPassportNumberAsync(string passportNumber)
        => await _context.Passengers
            .FirstOrDefaultAsync(p => p.PassportNumber == passportNumber.ToUpper());

    public async Task<PassengerInfo?> FindByTicketNumberAsync(string ticketNumber)
        => await _context.Passengers
            .FirstOrDefaultAsync(p => p.TicketNumber == ticketNumber);

    public async Task<PassengerInfo?> FindBySeatIdAsync(int seatId)
        => await _context.Passengers
            .FirstOrDefaultAsync(p => p.SeatId == seatId);

    public async Task<int> CountByBookingIdAsync(string bookingId)
        => await _context.Passengers
            .CountAsync(p => p.BookingId == bookingId);

    public async Task DeleteByBookingIdAsync(string bookingId)
    {
        var passengers = await _context.Passengers
            .Where(p => p.BookingId == bookingId)
            .ToListAsync();

        _context.Passengers.RemoveRange(passengers);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsByTicketNumberAsync(string ticketNumber)
        => await _context.Passengers
            .AnyAsync(p => p.TicketNumber == ticketNumber);

    public async Task<PassengerInfo> AddAsync(PassengerInfo passenger)
    {
        _context.Passengers.Add(passenger);
        await _context.SaveChangesAsync();
        return passenger;
    }

    public async Task<PassengerInfo> UpdateAsync(PassengerInfo passenger)
    {
        _context.Passengers.Update(passenger);
        await _context.SaveChangesAsync();
        return passenger;
    }

    public async Task DeleteAsync(int passengerId)
    {
        var passenger = await _context.Passengers.FindAsync(passengerId);
        if (passenger != null)
        {
            _context.Passengers.Remove(passenger);
            await _context.SaveChangesAsync();
        }
    }
}
