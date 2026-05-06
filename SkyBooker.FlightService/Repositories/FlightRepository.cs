using Microsoft.EntityFrameworkCore;
using SkyBooker.FlightService.Context;
using SkyBooker.FlightService.Entities;
using SkyBooker.FlightService.Enums;

namespace SkyBooker.FlightService.Repositories;

public class FlightRepository : IFlightRepository
{
    private readonly FlightDbContext _context;

    public FlightRepository(FlightDbContext context)
    {
        _context = context;
    }

    public async Task<Flight?> FindByFlightNumberAsync(string flightNumber)
        => await _context.Flights.FirstOrDefaultAsync(f => f.FlightNumber == flightNumber);

    public async Task<IList<Flight>> FindByOriginDestDateAsync(
        string origin, string destination, DateTime date)
    {
        var utcDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var nextDay  = utcDate.AddDays(1);
        return await _context.Flights
            .Where(f => f.OriginAirportCode == origin
                && f.DestinationAirportCode == destination
                && f.DepartureTime >= utcDate
                && f.DepartureTime < nextDay)
            .OrderBy(f => f.DepartureTime)
            .ToListAsync();
    }

    public async Task<IList<Flight>> FindByAirlineIdAsync(int airlineId)
        => await _context.Flights
            .Where(f => f.AirlineId == airlineId)
            .OrderBy(f => f.DepartureTime)
            .ToListAsync();

    public async Task<IList<Flight>> FindByStatusAsync(string status)
    {
        if (!Enum.TryParse<FlightStatus>(status, true, out var parsedStatus))
            return new List<Flight>();

        return await _context.Flights
            .Where(f => f.Status == parsedStatus)
            .ToListAsync();
    }

    public async Task<Flight?> FindByFlightIdAsync(int flightId)
        => await _context.Flights.FindAsync(flightId);

    public async Task<IList<Flight>> FindAvailableFlightsAsync(
        string origin, string destination, DateTime date)
    {
        var utcDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var nextDay  = utcDate.AddDays(1);
        return await _context.Flights
            .Where(f => f.OriginAirportCode == origin
                && f.DestinationAirportCode == destination
                && f.DepartureTime >= utcDate
                && f.DepartureTime < nextDay
                && f.AvailableSeats > 0
                && f.Status != FlightStatus.Cancelled)
            .OrderBy(f => f.BasePrice)
            .ToListAsync();
    }

    public async Task<int> CountByAirlineIdAsync(int airlineId)
        => await _context.Flights.CountAsync(f => f.AirlineId == airlineId);

    public async Task<Flight> AddAsync(Flight flight)
    {
        _context.Flights.Add(flight);
        await _context.SaveChangesAsync();
        return flight;
    }

    public async Task<Flight> UpdateAsync(Flight flight)
    {
        _context.Flights.Update(flight);
        await _context.SaveChangesAsync();
        return flight;
    }

    public async Task DeleteAsync(int flightId)
    {
        var flight = await _context.Flights.FindAsync(flightId);
        if (flight != null)
        {
            _context.Flights.Remove(flight);
            await _context.SaveChangesAsync();
        }
    }
}