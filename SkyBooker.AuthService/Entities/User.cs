using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkyBooker.AuthService.Enums;

namespace SkyBooker.AuthService.Entities;

[Table("users")]
public class User
{
    [Key]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("password_hash")]
    public string? PasswordHash { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("role")]
    public UserRole Role { get; set; } = UserRole.Passenger;

    [Column("provider")]
    public string Provider { get; set; } = "local";

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("passport_number")]
    public string? PassportNumber { get; set; }

    [Column("nationality")]
    public string? Nationality { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}