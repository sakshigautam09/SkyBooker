using FluentValidation;
using SkyBooker.PassengerService.DTOs;

namespace SkyBooker.PassengerService.Validators;

public class AddPassengerRequestValidator : AbstractValidator<AddPassengerRequestDto>
{
    private static readonly string[] ValidTitles = ["Mr", "Mrs", "Ms", "Dr", "Prof"];
    private static readonly string[] ValidGenders = ["Male", "Female", "Other"];

    public AddPassengerRequestValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty().WithMessage("BookingId is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .Must(t => ValidTitles.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Title must be one of: Mr, Mrs, Ms, Dr, Prof.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(50).WithMessage("First name must not exceed 50 characters.")
            .Matches(@"^[a-zA-Z\s\-']+$").WithMessage("First name can only contain letters, spaces, hyphens, and apostrophes.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(50).WithMessage("Last name must not exceed 50 characters.")
            .Matches(@"^[a-zA-Z\s\-']+$").WithMessage("Last name can only contain letters, spaces, hyphens, and apostrophes.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .LessThan(DateTime.UtcNow).WithMessage("Date of birth must be in the past.")
            .GreaterThan(DateTime.UtcNow.AddYears(-120)).WithMessage("Invalid date of birth.");

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("Gender is required.")
            .Must(g => ValidGenders.Contains(g, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Gender must be one of: Male, Female, Other.");

        RuleFor(x => x.PassportNumber)
            .NotEmpty().WithMessage("Passport number is required.")
            .MaximumLength(20).WithMessage("Passport number must not exceed 20 characters.")
            .Matches(@"^[A-Z0-9]+$").WithMessage("Passport number must contain only uppercase letters and digits.");

        RuleFor(x => x.Nationality)
            .NotEmpty().WithMessage("Nationality is required.")
            .MaximumLength(60).WithMessage("Nationality must not exceed 60 characters.");

        // Passport expiry must be a future date
        RuleFor(x => x.PassportExpiry)
            .NotEmpty().WithMessage("Passport expiry date is required.")
            .GreaterThan(DateTime.UtcNow).WithMessage("Passport must not be expired. Expiry date must be in the future.");

        RuleFor(x => x.AirlineCode)
            .NotEmpty().WithMessage("AirlineCode is required for ticket number generation.")
            .MaximumLength(3).WithMessage("Airline code must not exceed 3 characters.");

        RuleFor(x => x.FlightNumber)
            .NotEmpty().WithMessage("FlightNumber is required for ticket number generation.");
    }
}
