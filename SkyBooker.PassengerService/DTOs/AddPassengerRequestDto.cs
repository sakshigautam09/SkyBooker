namespace SkyBooker.PassengerService.DTOs;

public class AddPassengerRequestDto
{
    public string BookingId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string PassportNumber { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public DateTime PassportExpiry { get; set; }

    // For ticket number generation
    public string AirlineCode { get; set; } = string.Empty;
    public string FlightNumber { get; set; } = string.Empty;
}
