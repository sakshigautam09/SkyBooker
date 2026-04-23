namespace SkyBooker.AirlineService.DTOs;

// ── Airline DTOs ─────────────────────────────────────────────────────────────

public class AirlineResponseDto
{
    public int AirlineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IataCode { get; set; } = string.Empty;
    public string? IcaoCode { get; set; }
    public string? LogoUrl { get; set; }
    public string? Country { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; }
}

public class CreateAirlineRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string IataCode { get; set; } = string.Empty;
    public string? IcaoCode { get; set; }
    public string? LogoUrl { get; set; }
    public string? Country { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}

public class UpdateAirlineRequestDto
{
    public string? Name { get; set; }
    public string? IcaoCode { get; set; }
    public string? LogoUrl { get; set; }
    public string? Country { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}

// ── Airport DTOs ─────────────────────────────────────────────────────────────

public class AirportResponseDto
{
    public int AirportId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IataCode { get; set; } = string.Empty;
    public string? IcaoCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Timezone { get; set; }
}

public class CreateAirportRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string IataCode { get; set; } = string.Empty;
    public string? IcaoCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Timezone { get; set; }
}

public class UpdateAirportRequestDto
{
    public string? Name { get; set; }
    public string? IcaoCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Timezone { get; set; }
}
