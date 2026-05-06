using Moq;
using NUnit.Framework;
using SkyBooker.AirlineService.Services;
using SkyBooker.AirlineService.DTOs;
using SkyBooker.AirlineService.Validators;

namespace SkyBooker.Tests;

[TestFixture]
public class AirlineServiceTests
{
    private Mock<IAirlineService> _airlineServiceMock = null!;

    [SetUp]
    public void SetUp() => _airlineServiceMock = new Mock<IAirlineService>();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static AirlineResponseDto MakeAirlineDto(
        int id       = 1,
        string name  = "Air India",
        string iata  = "AI",
        bool isActive = true) => new()
    {
        AirlineId = id,
        Name      = name,
        IataCode  = iata,
        IsActive  = isActive
    };

    private static AirportResponseDto MakeAirportDto(
        int id      = 1,
        string iata = "DEL",
        string name = "Indira Gandhi International Airport",
        string city = "New Delhi") => new()
    {
        AirportId = id,
        IataCode  = iata,
        Name      = name,
        City      = city,
        Country   = "India"
    };

    // ─── CreateAirlineAsync ───────────────────────────────────────────────────

    [Test]
    public async Task CreateAirlineAsync_ValidRequest_ReturnsAirlineDto()
    {
        var dto = new CreateAirlineRequestDto
        {
            Name     = "Air India",
            IataCode = "AI",
            IcaoCode = "AIC",
            Country  = "India"
        };
        var expected = MakeAirlineDto();
        _airlineServiceMock.Setup(s => s.CreateAirlineAsync(dto)).ReturnsAsync(expected);

        var result = await _airlineServiceMock.Object.CreateAirlineAsync(dto);

        Assert.That(result.AirlineId, Is.EqualTo(1));
        Assert.That(result.Name, Is.EqualTo("Air India"));
        Assert.That(result.IataCode, Is.EqualTo("AI"));
    }

    // ─── GetAirlineByIdAsync ──────────────────────────────────────────────────

    [Test]
    public async Task GetAirlineByIdAsync_ExistingAirline_ReturnsDto()
    {
        _airlineServiceMock.Setup(s => s.GetAirlineByIdAsync(1)).ReturnsAsync(MakeAirlineDto());

        var result = await _airlineServiceMock.Object.GetAirlineByIdAsync(1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.AirlineId, Is.EqualTo(1));
    }

    [Test]
    public async Task GetAirlineByIdAsync_NonExistingAirline_ThrowsException()
    {
        _airlineServiceMock.Setup(s => s.GetAirlineByIdAsync(999))
            .ThrowsAsync(new KeyNotFoundException("Airline not found."));

        Assert.ThrowsAsync<KeyNotFoundException>(() => _airlineServiceMock.Object.GetAirlineByIdAsync(999));
    }

    // ─── GetAirlineByIataAsync ────────────────────────────────────────────────

    [Test]
    public async Task GetAirlineByIataAsync_ValidCode_ReturnsAirline()
    {
        _airlineServiceMock.Setup(s => s.GetAirlineByIataAsync("AI")).ReturnsAsync(MakeAirlineDto());

        var result = await _airlineServiceMock.Object.GetAirlineByIataAsync("AI");

        Assert.That(result.IataCode, Is.EqualTo("AI"));
    }

    // ─── GetAllAirlinesAsync ──────────────────────────────────────────────────

    [Test]
    public async Task GetAllAirlinesAsync_WithData_ReturnsAllAirlines()
    {
        var airlines = new List<AirlineResponseDto>
        {
            MakeAirlineDto(1, "Air India",  "AI"),
            MakeAirlineDto(2, "IndiGo",     "6E"),
            MakeAirlineDto(3, "SpiceJet",   "SG")
        };
        _airlineServiceMock.Setup(s => s.GetAllAirlinesAsync()).ReturnsAsync(airlines);

        var result = await _airlineServiceMock.Object.GetAllAirlinesAsync();

        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result.Any(a => a.IataCode == "6E"), Is.True);
    }

    // ─── UpdateAirlineAsync ───────────────────────────────────────────────────

    [Test]
    public async Task UpdateAirlineAsync_ValidUpdate_ReturnsUpdatedAirline()
    {
        var updateDto = new UpdateAirlineRequestDto { Name = "Air India Updated", IcaoCode = "AICD" };
        var expected  = MakeAirlineDto(name: "Air India Updated");

        _airlineServiceMock.Setup(s => s.UpdateAirlineAsync(1, updateDto)).ReturnsAsync(expected);

        var result = await _airlineServiceMock.Object.UpdateAirlineAsync(1, updateDto);

        Assert.That(result.Name, Is.EqualTo("Air India Updated"));
    }

    // ─── DeactivateAirlineAsync ───────────────────────────────────────────────

    [Test]
    public async Task DeactivateAirlineAsync_ActiveAirline_ReturnsInactiveAirline()
    {
        var expected = MakeAirlineDto(isActive: false);
        _airlineServiceMock.Setup(s => s.DeactivateAirlineAsync(1)).ReturnsAsync(expected);

        var result = await _airlineServiceMock.Object.DeactivateAirlineAsync(1);

        Assert.That(result.IsActive, Is.False);
    }

    // ─── CreateAirportAsync ───────────────────────────────────────────────────

    [Test]
    public async Task CreateAirportAsync_ValidRequest_ReturnsAirportDto()
    {
        var dto = new CreateAirportRequestDto
        {
            IataCode = "DEL",
            Name     = "Indira Gandhi International Airport",
            City     = "New Delhi",
            Country  = "India"
        };
        var expected = MakeAirportDto();
        _airlineServiceMock.Setup(s => s.CreateAirportAsync(dto)).ReturnsAsync(expected);

        var result = await _airlineServiceMock.Object.CreateAirportAsync(dto);

        Assert.That(result.AirportId, Is.EqualTo(1));
        Assert.That(result.IataCode, Is.EqualTo("DEL"));
    }

    // ─── GetAirportByIdAsync ──────────────────────────────────────────────────

    [Test]
    public async Task GetAirportByIdAsync_ExistingAirport_ReturnsDto()
    {
        _airlineServiceMock.Setup(s => s.GetAirportByIdAsync(1)).ReturnsAsync(MakeAirportDto());

        var result = await _airlineServiceMock.Object.GetAirportByIdAsync(1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.IataCode, Is.EqualTo("DEL"));
    }

    // ─── GetAirportByIataAsync ────────────────────────────────────────────────

    [Test]
    public async Task GetAirportByIataAsync_ValidCode_ReturnsAirport()
    {
        _airlineServiceMock.Setup(s => s.GetAirportByIataAsync("BOM")).ReturnsAsync(MakeAirportDto(iata: "BOM", name: "CSIA", city: "Mumbai"));

        var result = await _airlineServiceMock.Object.GetAirportByIataAsync("BOM");

        Assert.That(result.IataCode, Is.EqualTo("BOM"));
        Assert.That(result.City, Is.EqualTo("Mumbai"));
    }

    // ─── SearchAirportsAsync ──────────────────────────────────────────────────

    [Test]
    public async Task SearchAirportsAsync_ValidQuery_ReturnsMatchingAirports()
    {
        var airports = new List<AirportResponseDto>
        {
            MakeAirportDto(1, "DEL", "Indira Gandhi", "New Delhi"),
            MakeAirportDto(2, "BOM", "CSIA",          "Mumbai")
        };
        _airlineServiceMock.Setup(s => s.SearchAirportsAsync("India")).ReturnsAsync(airports);

        var result = await _airlineServiceMock.Object.SearchAirportsAsync("India");

        Assert.That(result.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task SearchAirportsAsync_NoMatch_ReturnsEmptyList()
    {
        _airlineServiceMock.Setup(s => s.SearchAirportsAsync("XYZ")).ReturnsAsync(new List<AirportResponseDto>());

        var result = await _airlineServiceMock.Object.SearchAirportsAsync("XYZ");

        Assert.That(result, Is.Empty);
    }

    // ─── GetAirportsByCityAsync ───────────────────────────────────────────────

    [Test]
    public async Task GetAirportsByCityAsync_ValidCity_ReturnsAirports()
    {
        var airports = new List<AirportResponseDto> { MakeAirportDto(city: "New Delhi") };
        _airlineServiceMock.Setup(s => s.GetAirportsByCityAsync("New Delhi")).ReturnsAsync(airports);

        var result = await _airlineServiceMock.Object.GetAirportsByCityAsync("New Delhi");

        Assert.That(result.All(a => a.City == "New Delhi"), Is.True);
    }

    // ─── GetAllAirportsAsync ──────────────────────────────────────────────────

    [Test]
    public async Task GetAllAirportsAsync_WithData_ReturnsAllAirports()
    {
        var airports = new List<AirportResponseDto>
        {
            MakeAirportDto(1, "DEL"),
            MakeAirportDto(2, "BOM"),
            MakeAirportDto(3, "BLR")
        };
        _airlineServiceMock.Setup(s => s.GetAllAirportsAsync()).ReturnsAsync(airports);

        var result = await _airlineServiceMock.Object.GetAllAirportsAsync();

        Assert.That(result.Count, Is.EqualTo(3));
    }
}

// ─── AirlineValidator Tests ───────────────────────────────────────────────────

[TestFixture]
public class CreateAirlineRequestValidatorTests
{
    private CreateAirlineRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateAirlineRequestValidator();

    [Test]
    public void Validate_ValidDto_PassesValidation()
    {
        var dto = new CreateAirlineRequestDto
        {
            Name     = "Air India",
            IataCode = "AI",
            IcaoCode = "AICD",
            Country  = "India"
        };
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_EmptyName_FailsValidation()
    {
        var dto = new CreateAirlineRequestDto { Name = "", IataCode = "AI" };
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "Name"), Is.True);
    }

    [Test]
    public void Validate_IataCodeTooLong_FailsValidation()
    {
        var dto = new CreateAirlineRequestDto { Name = "TestAir", IataCode = "ABCD" }; // max 3
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_IcaoCodeWrongLength_FailsValidation()
    {
        var dto = new CreateAirlineRequestDto { Name = "TestAir", IataCode = "TA", IcaoCode = "AB" }; // must be exactly 4
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_InvalidContactEmail_FailsValidation()
    {
        var dto = new CreateAirlineRequestDto { Name = "TestAir", IataCode = "TA", ContactEmail = "not-an-email" };
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_InvalidLogoUrl_FailsValidation()
    {
        var dto = new CreateAirlineRequestDto { Name = "TestAir", IataCode = "TA", LogoUrl = "not-a-url" };
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_ValidLogoUrl_PassesValidation()
    {
        var dto = new CreateAirlineRequestDto { Name = "TestAir", IataCode = "TA", LogoUrl = "https://example.com/logo.png" };
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_IataCodeWithSpecialChars_FailsValidation()
    {
        var dto = new CreateAirlineRequestDto { Name = "TestAir", IataCode = "A!" };
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.False);
    }
}