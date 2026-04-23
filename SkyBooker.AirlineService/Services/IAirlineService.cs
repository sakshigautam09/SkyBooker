using SkyBooker.AirlineService.DTOs;

namespace SkyBooker.AirlineService.Services;

public interface IAirlineService
{
    // Airline operations
    Task<AirlineResponseDto> CreateAirlineAsync(CreateAirlineRequestDto dto);
    Task<AirlineResponseDto> GetAirlineByIdAsync(int airlineId);
    Task<AirlineResponseDto> GetAirlineByIataAsync(string iataCode);
    Task<IList<AirlineResponseDto>> GetAllAirlinesAsync();
    Task<AirlineResponseDto> UpdateAirlineAsync(int airlineId, UpdateAirlineRequestDto dto);
    Task<AirlineResponseDto> DeactivateAirlineAsync(int airlineId);

    // Airport operations
    Task<AirportResponseDto> CreateAirportAsync(CreateAirportRequestDto dto);
    Task<AirportResponseDto> GetAirportByIdAsync(int airportId);
    Task<AirportResponseDto> GetAirportByIataAsync(string iataCode);
    Task<IList<AirportResponseDto>> SearchAirportsAsync(string query);
    Task<IList<AirportResponseDto>> GetAirportsByCityAsync(string city);
    Task<IList<AirportResponseDto>> GetAirportsByCountryAsync(string country);
    Task<IList<AirportResponseDto>> GetAllAirportsAsync();
    Task<AirportResponseDto> UpdateAirportAsync(int airportId, UpdateAirportRequestDto dto);
}
