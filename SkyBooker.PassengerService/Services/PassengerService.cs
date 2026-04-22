using FluentValidation;
using SkyBooker.PassengerService.DTOs;
using SkyBooker.PassengerService.Entities;
using SkyBooker.PassengerService.Enums;
using SkyBooker.PassengerService.Repositories;

namespace SkyBooker.PassengerService.Services;

public class PassengerService : IPassengerService
{
    private readonly IPassengerRepository _passengerRepository;
    private readonly IValidator<AddPassengerRequestDto> _validator;
    private readonly ILogger<PassengerService> _logger;

    public PassengerService(
        IPassengerRepository passengerRepository,
        IValidator<AddPassengerRequestDto> validator,
        ILogger<PassengerService> logger)
    {
        _passengerRepository = passengerRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<PassengerResponseDto> AddPassengerAsync(AddPassengerRequestDto dto)
    {
        // FluentValidation — passport expiry, age, mandatory fields
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            throw new ArgumentException(errors);
        }

        // Determine passenger type from date of birth
        var passengerType = DeterminePassengerType(dto.DateOfBirth);

        // Generate unique ticket number — format: {AirlineCode}{FlightNumber}-{random-6-digit}
        var ticketNumber = await GenerateUniqueTicketNumberAsync(dto.AirlineCode, dto.FlightNumber);

        var passenger = new PassengerInfo
        {
            BookingId = dto.BookingId,
            Title = dto.Title,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            PassportNumber = dto.PassportNumber.ToUpper(),
            Nationality = dto.Nationality,
            PassportExpiry = dto.PassportExpiry,
            PassengerType = passengerType,
            TicketNumber = ticketNumber,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _passengerRepository.AddAsync(passenger);

        _logger.LogInformation(
            "Passenger added: {FullName}, Type={Type}, Ticket={Ticket}, Booking={BookingId}",
            $"{dto.FirstName} {dto.LastName}", passengerType, ticketNumber, dto.BookingId);

        return MapToDto(created);
    }

    public async Task<PassengerResponseDto?> GetPassengerByIdAsync(int passengerId)
    {
        var passenger = await _passengerRepository.FindByPassengerIdAsync(passengerId);
        return passenger == null ? null : MapToDto(passenger);
    }

    public async Task<IList<PassengerResponseDto>> GetPassengersByBookingAsync(string bookingId)
    {
        var passengers = await _passengerRepository.FindByBookingIdAsync(bookingId);
        return passengers.Select(MapToDto).ToList();
    }

    public async Task<PassengerResponseDto?> GetByPassportNumberAsync(string passportNumber)
    {
        var passenger = await _passengerRepository.FindByPassportNumberAsync(passportNumber);
        return passenger == null ? null : MapToDto(passenger);
    }

    public async Task<PassengerResponseDto?> GetByTicketNumberAsync(string ticketNumber)
    {
        var passenger = await _passengerRepository.FindByTicketNumberAsync(ticketNumber);
        return passenger == null ? null : MapToDto(passenger);
    }

    public async Task<PassengerResponseDto> UpdatePassengerAsync(int passengerId, UpdatePassengerRequestDto dto)
    {
        var passenger = await _passengerRepository.FindByPassengerIdAsync(passengerId)
            ?? throw new KeyNotFoundException($"Passenger {passengerId} not found.");

        if (!string.IsNullOrEmpty(dto.Title)) passenger.Title = dto.Title;
        if (!string.IsNullOrEmpty(dto.FirstName)) passenger.FirstName = dto.FirstName.Trim();
        if (!string.IsNullOrEmpty(dto.LastName)) passenger.LastName = dto.LastName.Trim();
        if (!string.IsNullOrEmpty(dto.Gender)) passenger.Gender = dto.Gender;
        if (!string.IsNullOrEmpty(dto.PassportNumber))
            passenger.PassportNumber = dto.PassportNumber.ToUpper();
        if (!string.IsNullOrEmpty(dto.Nationality)) passenger.Nationality = dto.Nationality;
        if (dto.PassportExpiry.HasValue)
        {
            if (dto.PassportExpiry.Value <= DateTime.UtcNow)
                throw new ArgumentException("Passport expiry must be a future date.");
            passenger.PassportExpiry = dto.PassportExpiry.Value;
        }

        var updated = await _passengerRepository.UpdateAsync(passenger);
        return MapToDto(updated);
    }

    public async Task<PassengerResponseDto> AssignSeatAsync(int passengerId, AssignSeatRequestDto dto)
    {
        var passenger = await _passengerRepository.FindByPassengerIdAsync(passengerId)
            ?? throw new KeyNotFoundException($"Passenger {passengerId} not found.");

        passenger.SeatId = dto.SeatId;
        passenger.SeatNumber = dto.SeatNumber;

        var updated = await _passengerRepository.UpdateAsync(passenger);

        _logger.LogInformation(
            "Seat assigned: PassengerId={PassengerId}, Seat={SeatNumber}",
            passengerId, dto.SeatNumber);

        return MapToDto(updated);
    }

    public async Task DeletePassengerAsync(int passengerId)
    {
        var passenger = await _passengerRepository.FindByPassengerIdAsync(passengerId)
            ?? throw new KeyNotFoundException($"Passenger {passengerId} not found.");

        await _passengerRepository.DeleteAsync(passengerId);
    }

    public async Task DeletePassengersByBookingAsync(string bookingId)
        => await _passengerRepository.DeleteByBookingIdAsync(bookingId);

    public async Task<int> GetPassengerCountAsync(string bookingId)
        => await _passengerRepository.CountByBookingIdAsync(bookingId);

    public async Task<bool> ValidatePassengerDataAsync(AddPassengerRequestDto dto)
    {
        var result = await _validator.ValidateAsync(dto);
        return result.IsValid;
    }

    public string GenerateTicketNumber(string airlineCode, string flightNumber)
    {
        var random6 = Random.Shared.Next(100000, 999999).ToString();
        return $"{airlineCode.ToUpper()}{flightNumber}-{random6}";
    }

    // ── Private Helpers ────────────────────────────────────────────────────────

    private async Task<string> GenerateUniqueTicketNumberAsync(string airlineCode, string flightNumber)
    {
        string ticketNumber;
        int attempts = 0;
        const int maxAttempts = 10;

        do
        {
            ticketNumber = GenerateTicketNumber(airlineCode, flightNumber);
            attempts++;

            if (attempts >= maxAttempts)
                throw new InvalidOperationException("Failed to generate unique ticket number.");
        }
        while (await _passengerRepository.ExistsByTicketNumberAsync(ticketNumber));

        return ticketNumber;
    }

    private static PassengerType DeterminePassengerType(DateTime dateOfBirth)
    {
        var today = DateTime.UtcNow;
        var age = today.Year - dateOfBirth.Year;

        // Adjust if birthday hasn't occurred yet this year
        if (dateOfBirth.Date > today.AddYears(-age)) age--;

        return age switch
        {
            >= 12 => PassengerType.Adult,   // 12+ years
            >= 2 => PassengerType.Child,    // 2-11 years
            _ => PassengerType.Infant       // under 2 years
        };
    }

    private static int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.UtcNow;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }

    private static PassengerResponseDto MapToDto(PassengerInfo p) => new()
    {
        PassengerId = p.PassengerId,
        BookingId = p.BookingId,
        Title = p.Title,
        FirstName = p.FirstName,
        LastName = p.LastName,
        DateOfBirth = p.DateOfBirth,
        Gender = p.Gender,
        PassportNumber = p.PassportNumber,
        Nationality = p.Nationality,
        PassportExpiry = p.PassportExpiry,
        SeatId = p.SeatId,
        SeatNumber = p.SeatNumber,
        TicketNumber = p.TicketNumber,
        PassengerType = p.PassengerType.ToString(),
        AgeYears = CalculateAge(p.DateOfBirth),
        CreatedAt = p.CreatedAt
    };
}
