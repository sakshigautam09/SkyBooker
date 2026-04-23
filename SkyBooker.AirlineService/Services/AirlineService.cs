using SkyBooker.AirlineService.DTOs;
using SkyBooker.AirlineService.Entities;
using SkyBooker.AirlineService.Repositories;

namespace SkyBooker.AirlineService.Services;

public class AirlineService : IAirlineService
{
    private readonly IAirlineRepository _airlineRepository;
    private readonly ILogger<AirlineService> _logger;

    public AirlineService(IAirlineRepository airlineRepository, ILogger<AirlineService> logger)
    {
        _airlineRepository = airlineRepository;
        _logger = logger;
    }

    // ── Airline ──────────────────────────────────────────────────────────────

    public async Task<AirlineResponseDto> CreateAirlineAsync(CreateAirlineRequestDto dto)
    {
        var existing = await _airlineRepository.FindByIataCodeAsync(dto.IataCode);
        if (existing != null)
            throw new InvalidOperationException(
                $"Airline with IATA code '{dto.IataCode}' already exists.");

        var airline = new Airline
        {
            Name = dto.Name,
            IataCode = dto.IataCode.ToUpper(),
            IcaoCode = dto.IcaoCode?.ToUpper(),
            LogoUrl = dto.LogoUrl,
            Country = dto.Country,
            ContactEmail = dto.ContactEmail,
            ContactPhone = dto.ContactPhone,
            IsActive = true
        };

        var created = await _airlineRepository.CreateAirlineAsync(airline);
        _logger.LogInformation("Airline created: {IataCode} — {Name}", created.IataCode, created.Name);
        return MapAirlineToDto(created);
    }

    public async Task<AirlineResponseDto> GetAirlineByIdAsync(int airlineId)
    {
        var airline = await _airlineRepository.FindByAirlineIdAsync(airlineId)
            ?? throw new KeyNotFoundException($"Airline with ID {airlineId} not found.");
        return MapAirlineToDto(airline);
    }

    public async Task<AirlineResponseDto> GetAirlineByIataAsync(string iataCode)
    {
        var airline = await _airlineRepository.FindByIataCodeAsync(iataCode)
            ?? throw new KeyNotFoundException($"Airline with IATA code '{iataCode}' not found.");
        return MapAirlineToDto(airline);
    }

    public async Task<IList<AirlineResponseDto>> GetAllAirlinesAsync()
    {
        var airlines = await _airlineRepository.FindAllAirlinesAsync();
        return airlines.Select(MapAirlineToDto).ToList();
    }

    public async Task<AirlineResponseDto> UpdateAirlineAsync(int airlineId, UpdateAirlineRequestDto dto)
    {
        var airline = await _airlineRepository.FindByAirlineIdAsync(airlineId)
            ?? throw new KeyNotFoundException($"Airline with ID {airlineId} not found.");

        if (dto.Name != null) airline.Name = dto.Name;
        if (dto.IcaoCode != null) airline.IcaoCode = dto.IcaoCode.ToUpper();
        if (dto.LogoUrl != null) airline.LogoUrl = dto.LogoUrl;
        if (dto.Country != null) airline.Country = dto.Country;
        if (dto.ContactEmail != null) airline.ContactEmail = dto.ContactEmail;
        if (dto.ContactPhone != null) airline.ContactPhone = dto.ContactPhone;

        var updated = await _airlineRepository.UpdateAirlineAsync(airline);
        _logger.LogInformation("Airline updated: {AirlineId}", airlineId);
        return MapAirlineToDto(updated);
    }

    public async Task<AirlineResponseDto> DeactivateAirlineAsync(int airlineId)
    {
        var airline = await _airlineRepository.FindByAirlineIdAsync(airlineId)
            ?? throw new KeyNotFoundException($"Airline with ID {airlineId} not found.");

        airline.IsActive = false;
        var updated = await _airlineRepository.UpdateAirlineAsync(airline);
        _logger.LogInformation("Airline deactivated: {AirlineId}", airlineId);
        return MapAirlineToDto(updated);
    }

    // ── Airport ──────────────────────────────────────────────────────────────

    public async Task<AirportResponseDto> CreateAirportAsync(CreateAirportRequestDto dto)
    {
        var existing = await _airlineRepository.FindAirportByIataCodeAsync(dto.IataCode);
        if (existing != null)
            throw new InvalidOperationException(
                $"Airport with IATA code '{dto.IataCode}' already exists.");

        var airport = new Airport
        {
            Name = dto.Name,
            IataCode = dto.IataCode.ToUpper(),
            IcaoCode = dto.IcaoCode?.ToUpper(),
            City = dto.City,
            Country = dto.Country,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Timezone = dto.Timezone
        };

        var created = await _airlineRepository.CreateAirportAsync(airport);
        _logger.LogInformation("Airport created: {IataCode} — {Name}", created.IataCode, created.Name);
        return MapAirportToDto(created);
    }

    public async Task<AirportResponseDto> GetAirportByIdAsync(int airportId)
    {
        var airport = await _airlineRepository.FindAirportByIdAsync(airportId)
            ?? throw new KeyNotFoundException($"Airport with ID {airportId} not found.");
        return MapAirportToDto(airport);
    }

    public async Task<AirportResponseDto> GetAirportByIataAsync(string iataCode)
    {
        var airport = await _airlineRepository.FindAirportByIataCodeAsync(iataCode)
            ?? throw new KeyNotFoundException($"Airport with IATA code '{iataCode}' not found.");
        return MapAirportToDto(airport);
    }

    public async Task<IList<AirportResponseDto>> SearchAirportsAsync(string query)
    {
        var airports = await _airlineRepository.SearchAirportsAsync(query);
        return airports.Select(MapAirportToDto).ToList();
    }

    public async Task<IList<AirportResponseDto>> GetAirportsByCityAsync(string city)
    {
        var airports = await _airlineRepository.FindAirportsByCityAsync(city);
        return airports.Select(MapAirportToDto).ToList();
    }

    public async Task<IList<AirportResponseDto>> GetAirportsByCountryAsync(string country)
    {
        var airports = await _airlineRepository.FindAirportsByCountryAsync(country);
        return airports.Select(MapAirportToDto).ToList();
    }

    public async Task<IList<AirportResponseDto>> GetAllAirportsAsync()
    {
        var airports = await _airlineRepository.FindAllAirportsAsync();
        return airports.Select(MapAirportToDto).ToList();
    }

    public async Task<AirportResponseDto> UpdateAirportAsync(int airportId, UpdateAirportRequestDto dto)
    {
        var airport = await _airlineRepository.FindAirportByIdAsync(airportId)
            ?? throw new KeyNotFoundException($"Airport with ID {airportId} not found.");

        if (dto.Name != null) airport.Name = dto.Name;
        if (dto.IcaoCode != null) airport.IcaoCode = dto.IcaoCode.ToUpper();
        if (dto.City != null) airport.City = dto.City;
        if (dto.Country != null) airport.Country = dto.Country;
        if (dto.Latitude.HasValue) airport.Latitude = dto.Latitude.Value;
        if (dto.Longitude.HasValue) airport.Longitude = dto.Longitude.Value;
        if (dto.Timezone != null) airport.Timezone = dto.Timezone;

        var updated = await _airlineRepository.UpdateAirportAsync(airport);
        _logger.LogInformation("Airport updated: {AirportId}", airportId);
        return MapAirportToDto(updated);
    }

    // ── Mappers ───────────────────────────────────────────────────────────────

    private static AirlineResponseDto MapAirlineToDto(Airline a) => new()
    {
        AirlineId = a.AirlineId,
        Name = a.Name,
        IataCode = a.IataCode,
        IcaoCode = a.IcaoCode,
        LogoUrl = a.LogoUrl,
        Country = a.Country,
        ContactEmail = a.ContactEmail,
        ContactPhone = a.ContactPhone,
        IsActive = a.IsActive
    };

    private static AirportResponseDto MapAirportToDto(Airport ap) => new()
    {
        AirportId = ap.AirportId,
        Name = ap.Name,
        IataCode = ap.IataCode,
        IcaoCode = ap.IcaoCode,
        City = ap.City,
        Country = ap.Country,
        Latitude = ap.Latitude,
        Longitude = ap.Longitude,
        Timezone = ap.Timezone
    };
}
