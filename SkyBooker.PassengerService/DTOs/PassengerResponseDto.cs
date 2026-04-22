namespace SkyBooker.PassengerService.DTOs;

public class PassengerResponseDto
{
    public int PassengerId { get; set; }
    public string BookingId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string PassportNumber { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public DateTime PassportExpiry { get; set; }
    public int? SeatId { get; set; }
    public string? SeatNumber { get; set; }
    public string? TicketNumber { get; set; }
    public string PassengerType { get; set; } = string.Empty;
    public int AgeYears { get; set; }
    public DateTime CreatedAt { get; set; }
}
