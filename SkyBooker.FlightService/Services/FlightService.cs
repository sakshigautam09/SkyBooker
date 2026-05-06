using SkyBooker.FlightService.DTOs;
using SkyBooker.FlightService.Entities;
using SkyBooker.FlightService.Enums;
using SkyBooker.FlightService.Repositories;

namespace SkyBooker.FlightService.Services;

public class FlightService : IFlightService
{
    private readonly IFlightRepository _flightRepository;

    public FlightService(IFlightRepository flightRepository)
    {
        _flightRepository = flightRepository;
    }

    public async Task<FlightResponseDto> AddFlightAsync(AddFlightRequestDto dto)
    {
        var existing = await _flightRepository.FindByFlightNumberAsync(dto.FlightNumber);
        if (existing != null)
            throw new InvalidOperationException($"Flight {dto.FlightNumber} already exists.");

        var flight = new Flight
        {
            FlightNumber           = dto.FlightNumber,
            AirlineId              = dto.AirlineId,
            OriginAirportCode      = dto.OriginAirportCode.ToUpper(),
            DestinationAirportCode = dto.DestinationAirportCode.ToUpper(),
            DepartureTime          = DateTime.SpecifyKind(dto.DepartureTime, DateTimeKind.Utc),
            ArrivalTime            = DateTime.SpecifyKind(dto.ArrivalTime,   DateTimeKind.Utc),
            DurationMinutes        = (int)(dto.ArrivalTime - dto.DepartureTime).TotalMinutes,
            AircraftType           = dto.AircraftType,
            TotalSeats             = dto.TotalSeats,
            AvailableSeats         = dto.TotalSeats,
            BasePrice              = dto.BasePrice,
            Status                 = FlightStatus.Scheduled
        };

        var created = await _flightRepository.AddAsync(flight);
        return MapToDto(created);
    }

    public async Task<FlightResponseDto?> GetFlightByIdAsync(int flightId)
    {
        var flight = await _flightRepository.FindByFlightIdAsync(flightId);
        return flight == null ? null : MapToDto(flight);
    }

    public async Task<FlightResponseDto?> GetFlightByNumberAsync(string flightNumber)
    {
        var flight = await _flightRepository.FindByFlightNumberAsync(flightNumber);
        return flight == null ? null : MapToDto(flight);
    }

    public async Task<IList<FlightResponseDto>> SearchFlightsAsync(
        string origin, string destination, DateTime date)
    {
        var flights = await _flightRepository.FindAvailableFlightsAsync(
            origin.ToUpper(), destination.ToUpper(), date);
        return flights.Select(MapToDto).ToList();
    }

    public async Task<Dictionary<string, IList<FlightResponseDto>>> SearchRoundTripAsync(
        string origin, string destination, DateTime departureDate, DateTime returnDate)
    {
        var outbound = await _flightRepository.FindAvailableFlightsAsync(
            origin.ToUpper(), destination.ToUpper(), departureDate);

        var returnFlights = await _flightRepository.FindAvailableFlightsAsync(
            destination.ToUpper(), origin.ToUpper(), returnDate);

        return new Dictionary<string, IList<FlightResponseDto>>
        {
            ["outbound"] = outbound.Select(MapToDto).ToList(),
            ["return"]   = returnFlights.Select(MapToDto).ToList()
        };
    }

    public async Task<FlightResponseDto> UpdateFlightAsync(int flightId, UpdateFlightRequestDto dto)
    {
        var flight = await _flightRepository.FindByFlightIdAsync(flightId)
            ?? throw new KeyNotFoundException($"Flight {flightId} not found.");

        if (dto.DepartureTime.HasValue)
            flight.DepartureTime = DateTime.SpecifyKind(dto.DepartureTime.Value, DateTimeKind.Utc);
        if (dto.ArrivalTime.HasValue)
            flight.ArrivalTime = DateTime.SpecifyKind(dto.ArrivalTime.Value, DateTimeKind.Utc);
        if (!string.IsNullOrEmpty(dto.AircraftType))
            flight.AircraftType = dto.AircraftType;
        if (dto.BasePrice.HasValue)
            flight.BasePrice = dto.BasePrice.Value;

        if (dto.DepartureTime.HasValue || dto.ArrivalTime.HasValue)
            flight.DurationMinutes = (int)(flight.ArrivalTime - flight.DepartureTime).TotalMinutes;

        var updated = await _flightRepository.UpdateAsync(flight);
        return MapToDto(updated);
    }

    public async Task<FlightResponseDto> UpdateStatusAsync(int flightId, UpdateFlightStatusDto dto)
    {
        var flight = await _flightRepository.FindByFlightIdAsync(flightId)
            ?? throw new KeyNotFoundException($"Flight {flightId} not found.");

        if (!Enum.TryParse<FlightStatus>(dto.Status, true, out var newStatus))
            throw new ArgumentException($"Invalid flight status: {dto.Status}");

        flight.Status = newStatus;
        var updated = await _flightRepository.UpdateAsync(flight);
        return MapToDto(updated);
    }

    public async Task DecrementSeatsAsync(int flightId)
    {
        var flight = await _flightRepository.FindByFlightIdAsync(flightId)
            ?? throw new KeyNotFoundException($"Flight {flightId} not found.");

        if (flight.AvailableSeats <= 0)
            throw new InvalidOperationException("No available seats.");

        flight.AvailableSeats--;
        await _flightRepository.UpdateAsync(flight);
    }

    public async Task IncrementSeatsAsync(int flightId)
    {
        var flight = await _flightRepository.FindByFlightIdAsync(flightId)
            ?? throw new KeyNotFoundException($"Flight {flightId} not found.");

        if (flight.AvailableSeats >= flight.TotalSeats)
            throw new InvalidOperationException("Available seats already at maximum.");

        flight.AvailableSeats++;
        await _flightRepository.UpdateAsync(flight);
    }

    public async Task DeleteFlightAsync(int flightId)
    {
        var flight = await _flightRepository.FindByFlightIdAsync(flightId)
            ?? throw new KeyNotFoundException($"Flight {flightId} not found.");

        await _flightRepository.DeleteAsync(flightId);
    }

    public async Task<IList<FlightResponseDto>> GetFlightsByAirlineAsync(int airlineId)
    {
        var flights = await _flightRepository.FindByAirlineIdAsync(airlineId);
        return flights.Select(MapToDto).ToList();
    }

    private static FlightResponseDto MapToDto(Flight f) => new()
    {
        FlightId               = f.FlightId,
        FlightNumber           = f.FlightNumber,
        AirlineId              = f.AirlineId,
        OriginAirportCode      = f.OriginAirportCode,
        DestinationAirportCode = f.DestinationAirportCode,
        DepartureTime          = f.DepartureTime,
        ArrivalTime            = f.ArrivalTime,
        DurationMinutes        = f.DurationMinutes,
        Status                 = f.Status.ToString(),
        AircraftType           = f.AircraftType,
        TotalSeats             = f.TotalSeats,
        AvailableSeats         = f.AvailableSeats,
        BasePrice              = f.BasePrice
    };
}