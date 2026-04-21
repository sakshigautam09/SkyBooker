using Microsoft.EntityFrameworkCore;
using SkyBooker.SeatService.Context;
using SkyBooker.SeatService.Entities;
using SkyBooker.SeatService.Enums;

namespace SkyBooker.SeatService.Repositories;

public class SeatRepository : ISeatRepository
{
    private readonly SeatDbContext _context;

    public SeatRepository(SeatDbContext context)
    {
        _context = context;
    }

    public async Task<IList<Seat>> FindByFlightIdAsync(int flightId)
        => await _context.Seats
            .Where(s => s.FlightId == flightId)
            .OrderBy(s => s.Row).ThenBy(s => s.Column)
            .ToListAsync();

    public async Task<IList<Seat>> FindByFlightIdAndSeatClassAsync(int flightId, string seatClass)
    {
        if (!Enum.TryParse<SeatClass>(seatClass, true, out var parsedClass))
            return new List<Seat>();

        return await _context.Seats
            .Where(s => s.FlightId == flightId && s.SeatClass == parsedClass)
            .OrderBy(s => s.Row).ThenBy(s => s.Column)
            .ToListAsync();
    }

    public async Task<Seat?> FindBySeatIdAsync(int seatId)
        => await _context.Seats.FindAsync(seatId);

    public async Task<IList<Seat>> FindAvailableByFlightIdAsync(int flightId)
        => await _context.Seats
            .Where(s => s.FlightId == flightId && s.Status == SeatStatus.Available)
            .OrderBy(s => s.Row).ThenBy(s => s.Column)
            .ToListAsync();

    public async Task<Seat?> FindByFlightIdAndSeatNumberAsync(int flightId, string seatNumber)
        => await _context.Seats
            .FirstOrDefaultAsync(s => s.FlightId == flightId && s.SeatNumber == seatNumber);

    public async Task<int> CountAvailableByClassAsync(int flightId, string seatClass)
    {
        if (!Enum.TryParse<SeatClass>(seatClass, true, out var parsedClass))
            return 0;

        return await _context.Seats
            .CountAsync(s => s.FlightId == flightId
                && s.SeatClass == parsedClass
                && s.Status == SeatStatus.Available);
    }

    public async Task DeleteByFlightIdAsync(int flightId)
    {
        var seats = await _context.Seats
            .Where(s => s.FlightId == flightId)
            .ToListAsync();

        _context.Seats.RemoveRange(seats);
        await _context.SaveChangesAsync();
    }

    public async Task<IList<Seat>> AddRangeAsync(IList<Seat> seats)
    {
        await _context.Seats.AddRangeAsync(seats);
        await _context.SaveChangesAsync();
        return seats;
    }

    public async Task<Seat> UpdateAsync(Seat seat)
    {
        _context.Seats.Update(seat);
        await _context.SaveChangesAsync();
        return seat;
    }

    public async Task<IList<Seat>> FindHeldSeatsExpiredAsync(DateTime expiryThreshold)
        => await _context.Seats
            .Where(s => s.Status == SeatStatus.Held
                && s.HeldSince.HasValue
                && s.HeldSince.Value <= expiryThreshold)
            .ToListAsync();
}
