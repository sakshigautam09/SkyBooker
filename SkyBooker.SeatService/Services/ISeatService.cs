using SkyBooker.SeatService.DTOs;

namespace SkyBooker.SeatService.Services;

public interface ISeatService
{
    Task<IList<SeatResponseDto>> AddSeatsForFlightAsync(IList<AddSeatRequestDto> seats);
    Task<IList<SeatResponseDto>> GetAvailableSeatsAsync(int flightId);
    Task<IList<SeatResponseDto>> GetAvailableByClassAsync(int flightId, string seatClass);
    Task<SeatResponseDto?> GetSeatByIdAsync(int seatId);
    Task<SeatResponseDto> HoldSeatAsync(int seatId, int userId);
    Task<SeatResponseDto> ReleaseSeatAsync(int seatId);
    Task<SeatResponseDto> ConfirmSeatAsync(int seatId);
    Task<SeatResponseDto> UpdateSeatAsync(int seatId, UpdateSeatRequestDto dto);
    Task<IList<SeatResponseDto>> GetSeatMapAsync(int flightId);
    Task<int> CountAvailableByClassAsync(int flightId, string seatClass);
    Task DeleteSeatsForFlightAsync(int flightId);
}
