using FluentValidation;
using SkyBooker.AuthService.DTOs;

namespace SkyBooker.AuthService.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    private static readonly string[] AllowedRoles = ["Passenger", "AirlineStaff", "Admin"];

    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format. Example: john@example.com");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter (A-Z).")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter (a-z).")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit (0-9).")
            .Matches(@"[@$!%*?&]").WithMessage("Password must contain at least one special character (@$!%*?&).");

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[0-9]{7,15}$").WithMessage("Phone must be 7-15 digits, optionally starting with +. Example: +919876543210")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Role)
            .Must(role => role == null || AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Role must be one of: Passenger, AirlineStaff, Admin.");

        // If role is Admin, AdminSecretKey must be provided
        RuleFor(x => x.AdminSecretKey)
            .NotEmpty().WithMessage("AdminSecretKey is required when registering as Admin.")
            .When(x => string.Equals(x.Role, "Admin", StringComparison.OrdinalIgnoreCase));

        RuleFor(x => x.PassportNumber)
            .MaximumLength(20).WithMessage("Passport number must not exceed 20 characters.")
            .When(x => !string.IsNullOrEmpty(x.PassportNumber));

        RuleFor(x => x.Nationality)
            .MaximumLength(60).WithMessage("Nationality must not exceed 60 characters.")
            .When(x => !string.IsNullOrEmpty(x.Nationality));
    }
}
