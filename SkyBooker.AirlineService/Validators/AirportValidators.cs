using FluentValidation;
using SkyBooker.AirlineService.DTOs;

namespace SkyBooker.AirlineService.Validators;

public class CreateAirportRequestValidator : AbstractValidator<CreateAirportRequestDto>
{
    public CreateAirportRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Airport name is required.")
            .MaximumLength(150).WithMessage("Airport name must not exceed 150 characters.");

        RuleFor(x => x.IataCode)
            .NotEmpty().WithMessage("IATA code is required.")
            .Length(3).WithMessage("Airport IATA code must be exactly 3 characters.")
            .Matches("^[A-Za-z]+$").WithMessage("Airport IATA code must contain only letters.");

        RuleFor(x => x.IcaoCode)
            .Length(4).WithMessage("ICAO code must be exactly 4 characters.")
            .Matches("^[A-Za-z0-9]+$").WithMessage("ICAO code must be alphanumeric.")
            .When(x => !string.IsNullOrEmpty(x.IcaoCode));

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.");

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.City));

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Country));

        RuleFor(x => x.Timezone)
            .MaximumLength(60).WithMessage("Timezone must not exceed 60 characters.")
            .When(x => !string.IsNullOrEmpty(x.Timezone));
    }
}

public class UpdateAirportRequestValidator : AbstractValidator<UpdateAirportRequestDto>
{
    public UpdateAirportRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Airport name must not be empty.")
            .MaximumLength(150).WithMessage("Airport name must not exceed 150 characters.")
            .When(x => x.Name != null);

        RuleFor(x => x.IcaoCode)
            .Length(4).WithMessage("ICAO code must be exactly 4 characters.")
            .Matches("^[A-Za-z0-9]+$").WithMessage("ICAO code must be alphanumeric.")
            .When(x => !string.IsNullOrEmpty(x.IcaoCode));

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.")
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.")
            .When(x => x.Longitude.HasValue);

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.City));

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Country));

        RuleFor(x => x.Timezone)
            .MaximumLength(60).WithMessage("Timezone must not exceed 60 characters.")
            .When(x => !string.IsNullOrEmpty(x.Timezone));
    }
}
