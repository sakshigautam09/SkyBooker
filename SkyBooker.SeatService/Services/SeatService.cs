using Microsoft.EntityFrameworkCore;
using SkyBooker.SeatService.DTOs;
using SkyBooker.SeatService.Entities;
using SkyBooker.SeatService.Enums;
using SkyBooker.SeatService.Repositories;

namespace SkyBooker.SeatService.Services;

public class SeatService : ISeatService
{
    private readonly ISeatRepository _seatRepository;

    public SeatService(ISeatRepository seatRepository)
    {
        _seatRepository = seatRepository;
    }

    public async Task<IList<SeatResponseDto>> AddSeatsForFlightAsync(IList<AddSeatRequestDto> dtos)
    {
        var seats = dtos.Select(dto =>
        {
            if (!Enum.TryParse<SeatClass>(dto.SeatClass, true, out var seatClass))
                throw new ArgumentException($"Invalid seat class: {dto.SeatClass}");

            return new Seat
            {
                FlightId = dto.FlightId,
                SeatNumber = dto.SeatNumber,
                SeatClass = seatClass,
                Row = dto.Row,
                Column = dto.Column,
                IsWindow = dto.IsWindow,
                IsAisle = dto.IsAisle,
                HasExtraLegroom = dto.HasExtraLegroom,
                PriceMultiplier = dto.PriceMultiplier,
                Status = SeatStatus.Available
            };
        }).ToList();

        var created = await _seatRepository.AddRangeAsync(seats);
        return created.Select(MapToDto).ToList();
    }

    public async Task<IList<SeatResponseDto>> GetAvailableSeatsAsync(int flightId)
    {
        var seats = await _seatRepository.FindAvailableByFlightIdAsync(flightId);
        return seats.Select(MapToDto).ToList();
    }

    public async Task<IList<SeatResponseDto>> GetAvailableByClassAsync(int flightId, string seatClass)
    {
        var seats = await _seatRepository.FindByFlightIdAndSeatClassAsync(flightId, seatClass);
        return seats
            .Where(s => s.Status == SeatStatus.Available)
            .Select(MapToDto)
            .ToList();
    }

    public async Task<SeatResponseDto?> GetSeatByIdAsync(int seatId)
    {
        var seat = await _seatRepository.FindBySeatIdAsync(seatId);
        return seat == null ? null : MapToDto(seat);
    }

    public async Task<SeatResponseDto> HoldSeatAsync(int seatId, int userId)
    {
        var seat = await _seatRepository.FindBySeatIdAsync(seatId)
            ?? throw new KeyNotFoundException($"Seat {seatId} not found.");

        if (seat.Status != SeatStatus.Available)
            throw new InvalidOperationException($"Seat {seat.SeatNumber} is not available.");

        seat.Status = SeatStatus.Held;
        seat.HeldSince = DateTime.UtcNow;
        seat.HeldByUserId = userId;

        try
        {
            var updated = await _seatRepository.UpdateAsync(seat);
            return MapToDto(updated);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Two users tried to hold the same seat simultaneously
            throw new InvalidOperationException(
                $"Seat {seat.SeatNumber} was just taken by another user. Please select a different seat.");
        }
    }

    public async Task<SeatResponseDto> ReleaseSeatAsync(int seatId)
    {
        var seat = await _seatRepository.FindBySeatIdAsync(seatId)
            ?? throw new KeyNotFoundException($"Seat {seatId} not found.");

        seat.Status = SeatStatus.Available;
        seat.HeldSince = null;
        seat.HeldByUserId = null;

        var updated = await _seatRepository.UpdateAsync(seat);
        return MapToDto(updated);
    }

    public async Task<SeatResponseDto> ConfirmSeatAsync(int seatId)
    {
        var seat = await _seatRepository.FindBySeatIdAsync(seatId)
            ?? throw new KeyNotFoundException($"Seat {seatId} not found.");

        if (seat.Status != SeatStatus.Held)
            throw new InvalidOperationException(
                $"Seat {seat.SeatNumber} must be in HELD status before confirming.");

        seat.Status = SeatStatus.Confirmed;
        var updated = await _seatRepository.UpdateAsync(seat);
        return MapToDto(updated);
    }

    public async Task<SeatResponseDto> UpdateSeatAsync(int seatId, UpdateSeatRequestDto dto)
    {
        var seat = await _seatRepository.FindBySeatIdAsync(seatId)
            ?? throw new KeyNotFoundException($"Seat {seatId} not found.");

        if (dto.IsWindow.HasValue) seat.IsWindow = dto.IsWindow.Value;
        if (dto.IsAisle.HasValue) seat.IsAisle = dto.IsAisle.Value;
        if (dto.HasExtraLegroom.HasValue) seat.HasExtraLegroom = dto.HasExtraLegroom.Value;
        if (dto.PriceMultiplier.HasValue) seat.PriceMultiplier = dto.PriceMultiplier.Value;

        if (!string.IsNullOrEmpty(dto.SeatClass))
        {
            if (!Enum.TryParse<SeatClass>(dto.SeatClass, true, out var seatClass))
                throw new ArgumentException($"Invalid seat class: {dto.SeatClass}");
            seat.SeatClass = seatClass;
        }

        var updated = await _seatRepository.UpdateAsync(seat);
        return MapToDto(updated);
    }

    public async Task<IList<SeatResponseDto>> GetSeatMapAsync(int flightId)
    {
        var seats = await _seatRepository.FindByFlightIdAsync(flightId);
        return seats.Select(MapToDto).ToList();
    }

    public async Task<int> CountAvailableByClassAsync(int flightId, string seatClass)
        => await _seatRepository.CountAvailableByClassAsync(flightId, seatClass);

    public async Task DeleteSeatsForFlightAsync(int flightId)
        => await _seatRepository.DeleteByFlightIdAsync(flightId);

    private static SeatResponseDto MapToDto(Seat s) => new()
    {
        SeatId = s.SeatId,
        FlightId = s.FlightId,
        SeatNumber = s.SeatNumber,
        SeatClass = s.SeatClass.ToString(),
        Row = s.Row,
        Column = s.Column,
        IsWindow = s.IsWindow,
        IsAisle = s.IsAisle,
        HasExtraLegroom = s.HasExtraLegroom,
        Status = s.Status.ToString(),
        PriceMultiplier = s.PriceMultiplier,
        HeldSince = s.HeldSince,
        HeldByUserId = s.HeldByUserId
    };
}
