namespace SkyBooker.PassengerService.DTOs;

public class UpdatePassengerRequestDto
{
    public string? Title { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Gender { get; set; }
    public string? PassportNumber { get; set; }
    public string? Nationality { get; set; }
    public DateTime? PassportExpiry { get; set; }
}
