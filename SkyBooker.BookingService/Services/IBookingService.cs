using SkyBooker.BookingService.DTOs;

namespace SkyBooker.BookingService.Services;

public interface IBookingService
{
    Task<BookingResponseDto> CreateBookingAsync(CreateBookingRequestDto dto);
    Task<BookingResponseDto?> GetBookingByIdAsync(string bookingId);
    Task<BookingResponseDto?> GetBookingByPnrAsync(string pnrCode);
    Task<IList<BookingResponseDto>> GetBookingsByUserAsync(int userId);
    Task<IList<BookingResponseDto>> GetBookingsByFlightAsync(int flightId);
    Task<BookingResponseDto> CancelBookingAsync(string bookingId);
    Task<BookingResponseDto> UpdateStatusAsync(string bookingId, UpdateBookingStatusDto dto);
    Task<FareSummary> CalculateFareAsync(CalculateFareRequestDto dto);
    Task<BookingResponseDto> AddAddOnAsync(string bookingId, AddAddOnRequestDto dto);
    Task<IList<BookingResponseDto>> GetUpcomingBookingsAsync(int userId);
}
