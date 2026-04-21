namespace SkyBooker.AuthService.DTOs;

public class UpdateProfileRequestDto
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? PassportNumber { get; set; }
    public string? Nationality { get; set; }
}
