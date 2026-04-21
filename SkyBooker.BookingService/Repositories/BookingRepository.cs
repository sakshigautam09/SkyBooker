using Microsoft.EntityFrameworkCore;
using SkyBooker.BookingService.Context;
using SkyBooker.BookingService.Entities;
using SkyBooker.BookingService.Enums;

namespace SkyBooker.BookingService.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly BookingDbContext _context;

    public BookingRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<IList<Booking>> FindByUserIdAsync(int userId)
        => await _context.Bookings
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BookedAt)
            .ToListAsync();

    public async Task<Booking?> FindByPnrCodeAsync(string pnrCode)
        => await _context.Bookings
            .FirstOrDefaultAsync(b => b.PnrCode == pnrCode.ToUpper());

    public async Task<IList<Booking>> FindByFlightIdAsync(int flightId)
        => await _context.Bookings
            .Where(b => b.FlightId == flightId)
            .OrderByDescending(b => b.BookedAt)
            .ToListAsync();

    public async Task<IList<Booking>> FindByStatusAsync(string status)
    {
        if (!Enum.TryParse<BookingStatus>(status, true, out var parsedStatus))
            return new List<Booking>();

        return await _context.Bookings
            .Where(b => b.Status == parsedStatus)
            .OrderByDescending(b => b.BookedAt)
            .ToListAsync();
    }

    public async Task<Booking?> FindByBookingIdAsync(string bookingId)
        => await _context.Bookings.FindAsync(bookingId);

    public async Task<int> CountByFlightIdAndStatusAsync(int flightId, string status)
    {
        if (!Enum.TryParse<BookingStatus>(status, true, out var parsedStatus))
            return 0;

        return await _context.Bookings
            .CountAsync(b => b.FlightId == flightId && b.Status == parsedStatus);
    }

    public async Task<IList<Booking>> FindByUserIdAndStatusAsync(int userId, string status)
    {
        if (!Enum.TryParse<BookingStatus>(status, true, out var parsedStatus))
            return new List<Booking>();

        return await _context.Bookings
            .Where(b => b.UserId == userId && b.Status == parsedStatus)
            .OrderByDescending(b => b.BookedAt)
            .ToListAsync();
    }

    public async Task<bool> ExistsByPnrCodeAsync(string pnrCode)
        => await _context.Bookings.AnyAsync(b => b.PnrCode == pnrCode);

    public async Task<Booking> CreateAsync(Booking booking)
    {
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        return booking;
    }

    public async Task<Booking> UpdateAsync(Booking booking)
    {
        _context.Bookings.Update(booking);
        await _context.SaveChangesAsync();
        return booking;
    }
}
