using SkyBooker.FlightService.DTOs;

namespace SkyBooker.FlightService.Services;

public interface IFlightService
{
    Task<FlightResponseDto> AddFlightAsync(AddFlightRequestDto dto);
    Task<FlightResponseDto?> GetFlightByIdAsync(int flightId);
    Task<FlightResponseDto?> GetFlightByNumberAsync(string flightNumber);
    Task<IList<FlightResponseDto>> SearchFlightsAsync(string origin, string destination, DateTime date);
    Task<Dictionary<string, IList<FlightResponseDto>>> SearchRoundTripAsync(
        string origin, string destination, DateTime departureDate, DateTime returnDate);
    Task<FlightResponseDto> UpdateFlightAsync(int flightId, UpdateFlightRequestDto dto);
    Task<FlightResponseDto> UpdateStatusAsync(int flightId, UpdateFlightStatusDto dto);
    Task DecrementSeatsAsync(int flightId);
    Task IncrementSeatsAsync(int flightId);
    Task DeleteFlightAsync(int flightId);
    Task<IList<FlightResponseDto>> GetFlightsByAirlineAsync(int airlineId);
}
