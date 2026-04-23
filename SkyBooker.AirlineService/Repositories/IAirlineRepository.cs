using SkyBooker.AirlineService.Entities;

namespace SkyBooker.AirlineService.Repositories;

public interface IAirlineRepository
{
    // Airline queries
    Task<Airline?> FindByAirlineIdAsync(int airlineId);
    Task<Airline?> FindByIataCodeAsync(string iataCode);
    Task<IList<Airline>> FindByIsActiveAsync(bool isActive);
    Task<IList<Airline>> FindAllAirlinesAsync();
    Task<Airline> CreateAirlineAsync(Airline airline);
    Task<Airline> UpdateAirlineAsync(Airline airline);

    // Airport queries
    Task<Airport?> FindAirportByIataCodeAsync(string iataCode);
    Task<IList<Airport>> FindAirportsByCityAsync(string city);
    Task<IList<Airport>> FindAirportsByCountryAsync(string country);

    /// <summary>Autocomplete search using EF.Functions.Like on name, iataCode, and city.</summary>
    Task<IList<Airport>> SearchAirportsAsync(string query);

    Task<Airport?> FindAirportByIdAsync(int airportId);
    Task<IList<Airport>> FindAllAirportsAsync();
    Task<Airport> CreateAirportAsync(Airport airport);
    Task<Airport> UpdateAirportAsync(Airport airport);
}
