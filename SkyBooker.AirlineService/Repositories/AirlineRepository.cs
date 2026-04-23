using Microsoft.EntityFrameworkCore;
using SkyBooker.AirlineService.Context;
using SkyBooker.AirlineService.Entities;

namespace SkyBooker.AirlineService.Repositories;

public class AirlineRepository : IAirlineRepository
{
    private readonly AirlineDbContext _context;

    public AirlineRepository(AirlineDbContext context)
    {
        _context = context;
    }

    // ── Airline ──────────────────────────────────────────────────────────────

    public async Task<Airline?> FindByAirlineIdAsync(int airlineId)
        => await _context.Airlines.FindAsync(airlineId);

    public async Task<Airline?> FindByIataCodeAsync(string iataCode)
        => await _context.Airlines
            .FirstOrDefaultAsync(a => a.IataCode.ToUpper() == iataCode.ToUpper());

    public async Task<IList<Airline>> FindByIsActiveAsync(bool isActive)
        => await _context.Airlines
            .Where(a => a.IsActive == isActive)
            .OrderBy(a => a.Name)
            .ToListAsync();

    public async Task<IList<Airline>> FindAllAirlinesAsync()
        => await _context.Airlines
            .OrderBy(a => a.Name)
            .ToListAsync();

    public async Task<Airline> CreateAirlineAsync(Airline airline)
    {
        _context.Airlines.Add(airline);
        await _context.SaveChangesAsync();
        return airline;
    }

    public async Task<Airline> UpdateAirlineAsync(Airline airline)
    {
        _context.Airlines.Update(airline);
        await _context.SaveChangesAsync();
        return airline;
    }

    // ── Airport ──────────────────────────────────────────────────────────────

    public async Task<Airport?> FindAirportByIataCodeAsync(string iataCode)
        => await _context.Airports
            .FirstOrDefaultAsync(ap => ap.IataCode.ToUpper() == iataCode.ToUpper());

    public async Task<IList<Airport>> FindAirportsByCityAsync(string city)
        => await _context.Airports
            .Where(ap => ap.City != null &&
                         EF.Functions.Like(ap.City.ToLower(), $"%{city.ToLower()}%"))
            .OrderBy(ap => ap.Name)
            .ToListAsync();

    public async Task<IList<Airport>> FindAirportsByCountryAsync(string country)
        => await _context.Airports
            .Where(ap => ap.Country != null &&
                         EF.Functions.Like(ap.Country.ToLower(), $"%{country.ToLower()}%"))
            .OrderBy(ap => ap.Name)
            .ToListAsync();

    public async Task<IList<Airport>> SearchAirportsAsync(string query)
    {
        var pattern = $"%{query.ToLower()}%";
        return await _context.Airports
            .Where(ap =>
                EF.Functions.Like(ap.Name.ToLower(), pattern) ||
                EF.Functions.Like(ap.IataCode.ToLower(), pattern) ||
                (ap.City != null && EF.Functions.Like(ap.City.ToLower(), pattern)))
            .OrderBy(ap => ap.Name)
            .Take(20)
            .ToListAsync();
    }

    public async Task<Airport?> FindAirportByIdAsync(int airportId)
        => await _context.Airports.FindAsync(airportId);

    public async Task<IList<Airport>> FindAllAirportsAsync()
        => await _context.Airports
            .OrderBy(ap => ap.Name)
            .ToListAsync();

    public async Task<Airport> CreateAirportAsync(Airport airport)
    {
        _context.Airports.Add(airport);
        await _context.SaveChangesAsync();
        return airport;
    }

    public async Task<Airport> UpdateAirportAsync(Airport airport)
    {
        _context.Airports.Update(airport);
        await _context.SaveChangesAsync();
        return airport;
    }
}
