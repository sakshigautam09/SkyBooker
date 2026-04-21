using SkyBooker.FlightService.Entities;

namespace SkyBooker.FlightService.Repositories;

public interface IFlightRepository
{
    Task<Flight?> FindByFlightNumberAsync(string flightNumber);
    Task<IList<Flight>> FindByOriginDestDateAsync(string origin, string destination, DateTime date);
    Task<IList<Flight>> FindByAirlineIdAsync(int airlineId);
    Task<IList<Flight>> FindByStatusAsync(string status);
    Task<Flight?> FindByFlightIdAsync(int flightId);
    Task<IList<Flight>> FindAvailableFlightsAsync(string origin, string destination, DateTime date);
    Task<int> CountByAirlineIdAsync(int airlineId);
    Task<Flight> AddAsync(Flight flight);
    Task<Flight> UpdateAsync(Flight flight);
    Task DeleteAsync(int flightId);
}
