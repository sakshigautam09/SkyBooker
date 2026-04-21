namespace SkyBooker.AuthService.DTOs;

public class AssignRoleRequestDto
{
    /// <summary>ID of the user to assign the role to</summary>
    /// <example>5</example>
    public int UserId { get; set; }

    /// <summary>Role to assign: Passenger, AirlineStaff, Admin</summary>
    /// <example>Admin</example>
    public string Role { get; set; } = string.Empty;
}
