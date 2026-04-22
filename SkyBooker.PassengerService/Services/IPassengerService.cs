using SkyBooker.PassengerService.DTOs;

namespace SkyBooker.PassengerService.Services;

public interface IPassengerService
{
    Task<PassengerResponseDto> AddPassengerAsync(AddPassengerRequestDto dto);
    Task<PassengerResponseDto?> GetPassengerByIdAsync(int passengerId);
    Task<IList<PassengerResponseDto>> GetPassengersByBookingAsync(string bookingId);
    Task<PassengerResponseDto?> GetByPassportNumberAsync(string passportNumber);
    Task<PassengerResponseDto?> GetByTicketNumberAsync(string ticketNumber);
    Task<PassengerResponseDto> UpdatePassengerAsync(int passengerId, UpdatePassengerRequestDto dto);
    Task<PassengerResponseDto> AssignSeatAsync(int passengerId, AssignSeatRequestDto dto);
    Task DeletePassengerAsync(int passengerId);
    Task DeletePassengersByBookingAsync(string bookingId);
    Task<int> GetPassengerCountAsync(string bookingId);
    Task<bool> ValidatePassengerDataAsync(AddPassengerRequestDto dto);
    string GenerateTicketNumber(string airlineCode, string flightNumber);
}
