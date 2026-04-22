namespace SkyBooker.PassengerService.DTOs;

public class AssignSeatRequestDto
{
    public int SeatId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
}
