namespace SkyBooker.AuthService.DTOs;

public class RegisterRequestDto
{
    /// <example>John Doe</example>
    public string FullName { get; set; } = string.Empty;

    /// <example>john@example.com</example>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Min 8 chars, at least 1 uppercase, 1 lowercase, 1 digit, 1 special character (@$!%*?&amp;)
    /// </summary>
    /// <example>Secret@123</example>
    public string Password { get; set; } = string.Empty;

    /// <example>+919876543210</example>
    public string? Phone { get; set; }

    /// <summary>
    /// Allowed values: Passenger, AirlineStaff, Admin.
    /// If registering as Admin, you must also provide the correct AdminSecretKey.
    /// </summary>
    /// <example>Passenger</example>
    public string? Role { get; set; }

    /// <summary>
    /// Required only when Role is Admin. Must match the server-configured admin secret.
    /// </summary>
    /// <example>SKYBOOKER_ADMIN_2024</example>
    public string? AdminSecretKey { get; set; }

    /// <example>A1234567</example>
    public string? PassportNumber { get; set; }

    /// <example>Indian</example>
    public string? Nationality { get; set; }
}
