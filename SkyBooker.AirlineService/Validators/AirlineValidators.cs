using FluentValidation;
using SkyBooker.AirlineService.DTOs;

namespace SkyBooker.AirlineService.Validators;

public class CreateAirlineRequestValidator : AbstractValidator<CreateAirlineRequestDto>
{
    public CreateAirlineRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Airline name is required.")
            .MaximumLength(100).WithMessage("Airline name must not exceed 100 characters.");

        RuleFor(x => x.IataCode)
            .NotEmpty().WithMessage("IATA code is required.")
            .Length(2, 3).WithMessage("IATA code must be 2 or 3 characters.")
            .Matches("^[A-Za-z0-9]+$").WithMessage("IATA code must be alphanumeric.");

        RuleFor(x => x.IcaoCode)
            .Length(4).WithMessage("ICAO code must be exactly 4 characters.")
            .Matches("^[A-Za-z0-9]+$").WithMessage("ICAO code must be alphanumeric.")
            .When(x => !string.IsNullOrEmpty(x.IcaoCode));

        RuleFor(x => x.ContactEmail)
            .EmailAddress().WithMessage("Contact email must be a valid email address.")
            .When(x => !string.IsNullOrEmpty(x.ContactEmail));

        RuleFor(x => x.LogoUrl)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Logo URL must be a valid absolute URL.")
            .When(x => !string.IsNullOrEmpty(x.LogoUrl));
    }
}

public class UpdateAirlineRequestValidator : AbstractValidator<UpdateAirlineRequestDto>
{
    public UpdateAirlineRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Airline name must not be empty.")
            .MaximumLength(100).WithMessage("Airline name must not exceed 100 characters.")
            .When(x => x.Name != null);

        RuleFor(x => x.IcaoCode)
            .Length(4).WithMessage("ICAO code must be exactly 4 characters.")
            .Matches("^[A-Za-z0-9]+$").WithMessage("ICAO code must be alphanumeric.")
            .When(x => !string.IsNullOrEmpty(x.IcaoCode));

        RuleFor(x => x.ContactEmail)
            .EmailAddress().WithMessage("Contact email must be a valid email address.")
            .When(x => !string.IsNullOrEmpty(x.ContactEmail));

        RuleFor(x => x.LogoUrl)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Logo URL must be a valid absolute URL.")
            .When(x => !string.IsNullOrEmpty(x.LogoUrl));
    }
}
