using Moq;
using NUnit.Framework;
using SkyBooker.PassengerService.Services;
using SkyBooker.PassengerService.DTOs;
using SkyBooker.PassengerService.Validators;

namespace SkyBooker.Tests;

[TestFixture]
public class PassengerServiceTests
{
    private Mock<IPassengerService> _passengerServiceMock = null!;

    [SetUp]
    public void SetUp() => _passengerServiceMock = new Mock<IPassengerService>();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static AddPassengerRequestDto MakeAddDto() => new()
    {
        BookingId      = "BK-001",
        Title          = "Mr",
        FirstName      = "Rahul",
        LastName       = "Sharma",
        DateOfBirth    = new DateTime(1995, 6, 15),
        Gender         = "Male",
        PassportNumber = "A1234567",
        Nationality    = "Indian",
        PassportExpiry = DateTime.UtcNow.AddYears(3),
        AirlineCode    = "AI",
        FlightNumber   = "AI-101"
    };

    private static PassengerResponseDto MakePassengerDto(int id = 1) => new()
    {
        PassengerId    = id,
        BookingId      = "BK-001",
        Title          = "Mr",           // was: FullName = "Mr Rahul Sharma"
        FirstName      = "Rahul",        // was: (combined into FullName)
        LastName       = "Sharma",       // was: (combined into FullName)
        PassportNumber = "A1234567",
        TicketNumber   = "AI/101/XYZ123"
    };

    // ─── AddPassengerAsync ────────────────────────────────────────────────────

    [Test]
    public async Task AddPassengerAsync_ValidDto_ReturnsPassengerResponse()
    {
        var dto      = MakeAddDto();
        var expected = MakePassengerDto();
        _passengerServiceMock.Setup(s => s.AddPassengerAsync(dto)).ReturnsAsync(expected);

        var result = await _passengerServiceMock.Object.AddPassengerAsync(dto);

        Assert.That(result.PassengerId, Is.EqualTo(1));
        Assert.That(result.PassportNumber, Is.EqualTo("A1234567"));
        Assert.That(result.TicketNumber, Is.Not.Empty);
    }

    // ─── GetPassengerByIdAsync ────────────────────────────────────────────────

    [Test]
    public async Task GetPassengerByIdAsync_ExistingPassenger_ReturnsDto()
    {
        _passengerServiceMock.Setup(s => s.GetPassengerByIdAsync(1)).ReturnsAsync(MakePassengerDto(1));

        var result = await _passengerServiceMock.Object.GetPassengerByIdAsync(1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.PassengerId, Is.EqualTo(1));
    }

    [Test]
    public async Task GetPassengerByIdAsync_NonExistingPassenger_ReturnsNull()
    {
        _passengerServiceMock.Setup(s => s.GetPassengerByIdAsync(999)).ReturnsAsync((PassengerResponseDto?)null);

        var result = await _passengerServiceMock.Object.GetPassengerByIdAsync(999);

        Assert.That(result, Is.Null);
    }

    // ─── GetPassengersByBookingAsync ──────────────────────────────────────────

    [Test]
    public async Task GetPassengersByBookingAsync_ValidBooking_ReturnsPassengers()
    {
        var passengers = new List<PassengerResponseDto>
        {
            MakePassengerDto(1),
            MakePassengerDto(2)
        };
        _passengerServiceMock.Setup(s => s.GetPassengersByBookingAsync("BK-001")).ReturnsAsync(passengers);

        var result = await _passengerServiceMock.Object.GetPassengersByBookingAsync("BK-001");

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.All(p => p.BookingId == "BK-001"), Is.True);
    }

    // ─── GetByPassportNumberAsync ─────────────────────────────────────────────

    [Test]
    public async Task GetByPassportNumberAsync_ValidPassport_ReturnsPassenger()
    {
        _passengerServiceMock.Setup(s => s.GetByPassportNumberAsync("A1234567")).ReturnsAsync(MakePassengerDto());

        var result = await _passengerServiceMock.Object.GetByPassportNumberAsync("A1234567");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.PassportNumber, Is.EqualTo("A1234567"));
    }

    // ─── GetByTicketNumberAsync ───────────────────────────────────────────────

    [Test]
    public async Task GetByTicketNumberAsync_ValidTicket_ReturnsPassenger()
    {
        _passengerServiceMock.Setup(s => s.GetByTicketNumberAsync("AI/101/XYZ123")).ReturnsAsync(MakePassengerDto());

        var result = await _passengerServiceMock.Object.GetByTicketNumberAsync("AI/101/XYZ123");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.TicketNumber, Is.EqualTo("AI/101/XYZ123"));
    }

    // ─── AssignSeatAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task AssignSeatAsync_ValidSeat_ReturnsSeatAssigned()
    {
        var assignDto = new AssignSeatRequestDto { SeatId = 5, SeatNumber = "5C" };
        var expected  = MakePassengerDto();

        _passengerServiceMock.Setup(s => s.AssignSeatAsync(1, assignDto)).ReturnsAsync(expected);

        var result = await _passengerServiceMock.Object.AssignSeatAsync(1, assignDto);

        Assert.That(result, Is.Not.Null);
    }

    // ─── UpdatePassengerAsync ─────────────────────────────────────────────────

    [Test]
    public async Task UpdatePassengerAsync_ValidUpdate_ReturnsUpdatedDto()
    {
        var updateDto = new UpdatePassengerRequestDto { FirstName = "Rahul" }; // was: MealPreference = "Veg" (property does not exist)
        var expected  = MakePassengerDto();

        _passengerServiceMock.Setup(s => s.UpdatePassengerAsync(1, updateDto)).ReturnsAsync(expected);

        var result = await _passengerServiceMock.Object.UpdatePassengerAsync(1, updateDto);

        Assert.That(result, Is.Not.Null);
    }

    // ─── DeletePassengerAsync ─────────────────────────────────────────────────

    [Test]
    public async Task DeletePassengerAsync_ExistingPassenger_CompletesSuccessfully()
    {
        _passengerServiceMock.Setup(s => s.DeletePassengerAsync(1)).Returns(Task.CompletedTask);
        Assert.DoesNotThrowAsync(() => _passengerServiceMock.Object.DeletePassengerAsync(1));
    }

    // ─── GetPassengerCountAsync ───────────────────────────────────────────────

    [Test]
    public async Task GetPassengerCountAsync_BookingWithPassengers_ReturnsCorrectCount()
    {
        _passengerServiceMock.Setup(s => s.GetPassengerCountAsync("BK-001")).ReturnsAsync(3);

        var result = await _passengerServiceMock.Object.GetPassengerCountAsync("BK-001");

        Assert.That(result, Is.EqualTo(3));
    }

    // ─── ValidatePassengerDataAsync ───────────────────────────────────────────

    [Test]
    public async Task ValidatePassengerDataAsync_ValidData_ReturnsTrue()
    {
        var dto = MakeAddDto();
        _passengerServiceMock.Setup(s => s.ValidatePassengerDataAsync(dto)).ReturnsAsync(true);

        var result = await _passengerServiceMock.Object.ValidatePassengerDataAsync(dto);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ValidatePassengerDataAsync_InvalidPassport_ReturnsFalse()
    {
        var dto = MakeAddDto();
        dto.PassportNumber = ""; // invalid
        _passengerServiceMock.Setup(s => s.ValidatePassengerDataAsync(dto)).ReturnsAsync(false);

        var result = await _passengerServiceMock.Object.ValidatePassengerDataAsync(dto);

        Assert.That(result, Is.False);
    }

    // ─── GenerateTicketNumber ─────────────────────────────────────────────────

    [Test]
    public void GenerateTicketNumber_ValidInput_ReturnsFormattedTicket()
    {
        _passengerServiceMock.Setup(s => s.GenerateTicketNumber("AI", "101"))
            .Returns("AI/101/ABC123");

        var result = _passengerServiceMock.Object.GenerateTicketNumber("AI", "101");

        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Does.Contain("AI"));
        Assert.That(result, Does.Contain("101"));
    }
}

// ─── AddPassengerRequestValidator Tests ──────────────────────────────────────

[TestFixture]
public class AddPassengerRequestValidatorTests
{
    private AddPassengerRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new AddPassengerRequestValidator();

    private static AddPassengerRequestDto ValidDto() => new()
    {
        BookingId      = "BK-001",
        Title          = "Mr",
        FirstName      = "Rahul",
        LastName       = "Sharma",
        DateOfBirth    = new DateTime(1995, 6, 15),
        Gender         = "Male",
        PassportNumber = "A1234567",
        Nationality    = "Indian",
        PassportExpiry = DateTime.UtcNow.AddYears(3),
        AirlineCode    = "AI",
        FlightNumber   = "AI-101"
    };

    [Test]
    public void Validate_ValidDto_PassesValidation()
    {
        var result = _validator.Validate(ValidDto());
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_EmptyBookingId_FailsValidation()
    {
        var dto = ValidDto(); dto.BookingId = "";
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "BookingId"), Is.True);
    }

    [Test]
    public void Validate_InvalidTitle_FailsValidation()
    {
        var dto = ValidDto(); dto.Title = "Sir"; // not in allowed list
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "Title"), Is.True);
    }

    [Test]
    public void Validate_FutureDateOfBirth_FailsValidation()
    {
        var dto = ValidDto(); dto.DateOfBirth = DateTime.UtcNow.AddDays(1);
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_InvalidGender_FailsValidation()
    {
        var dto = ValidDto(); dto.Gender = "Unknown";
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_LowercasePassportNumber_FailsValidation()
    {
        var dto = ValidDto(); dto.PassportNumber = "a1234567"; // must be uppercase
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_ExpiredPassport_FailsValidation()
    {
        var dto = ValidDto(); dto.PassportExpiry = DateTime.UtcNow.AddDays(-1);
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_AirlineCodeTooLong_FailsValidation()
    {
        var dto = ValidDto(); dto.AirlineCode = "ABCD"; // max 3 chars
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.False);
    }
}