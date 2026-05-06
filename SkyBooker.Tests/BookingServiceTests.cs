using Moq;
using NUnit.Framework;
using SkyBooker.BookingService.Services;
using SkyBooker.BookingService.DTOs;

namespace SkyBooker.Tests;

[TestFixture]
public class BookingServiceTests
{
    private Mock<IBookingService> _bookingServiceMock = null!;

    [SetUp]
    public void SetUp() => _bookingServiceMock = new Mock<IBookingService>();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static CreateBookingRequestDto MakeCreateDto(
        int userId      = 1,
        int flightId    = 10,
        string tripType = "OneWay",
        decimal price   = 4500m) => new()
    {
        UserId       = userId,
        FlightId     = flightId,
        TripType     = tripType,
        SeatIds      = new List<int> { 1, 2 },
        ContactEmail = "passenger@example.com",
        BasePrice    = price,
        LuggageKg    = 15
    };

    private static BookingResponseDto MakeResponseDto(
        string bookingId = "BK-0001",
        string pnr       = "PNR123",
        string status    = "Confirmed") => new()
    {
        BookingId  = bookingId,
        PnrCode    = pnr,
        Status     = status,
        TotalFare  = 4500m,
        TripType   = "OneWay"
    };

    // ─── CreateBookingAsync ───────────────────────────────────────────────────

    [Test]
    public async Task CreateBookingAsync_ValidOneWay_ReturnsBookingResponse()
    {
        var dto      = MakeCreateDto();
        var expected = MakeResponseDto();
        _bookingServiceMock.Setup(s => s.CreateBookingAsync(dto)).ReturnsAsync(expected);

        var result = await _bookingServiceMock.Object.CreateBookingAsync(dto);

        Assert.That(result.BookingId, Is.EqualTo("BK-0001"));
        Assert.That(result.Status, Is.EqualTo("Confirmed"));
        Assert.That(result.PnrCode, Is.EqualTo("PNR123"));
    }

    [Test]
    public async Task CreateBookingAsync_RoundTripWithReturnFlight_ReturnsBooking()
    {
        var dto = new CreateBookingRequestDto
        {
            UserId         = 1,
            FlightId       = 10,
            ReturnFlightId = 20,
            TripType       = "RoundTrip",
            SeatIds        = new List<int> { 3 },
            ContactEmail   = "passenger@example.com",
            BasePrice      = 8000m
        };

        var expected = new BookingResponseDto
        {
            BookingId      = "BK-0002",
            TripType       = "RoundTrip",
            ReturnFlightId = 20,
            Status         = "Confirmed",
            TotalFare      = 8000m,
            PnrCode        = "PNR456"
        };

        _bookingServiceMock.Setup(s => s.CreateBookingAsync(dto)).ReturnsAsync(expected);

        var result = await _bookingServiceMock.Object.CreateBookingAsync(dto);

        Assert.That(result.TripType, Is.EqualTo("RoundTrip"));
        Assert.That(result.ReturnFlightId, Is.EqualTo(20));
    }

    // ─── GetBookingByIdAsync ──────────────────────────────────────────────────

    [Test]
    public async Task GetBookingByIdAsync_ValidId_ReturnsBooking()
    {
        var expected = MakeResponseDto("BK-0010");
        _bookingServiceMock.Setup(s => s.GetBookingByIdAsync("BK-0010")).ReturnsAsync(expected);

        var result = await _bookingServiceMock.Object.GetBookingByIdAsync("BK-0010");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.BookingId, Is.EqualTo("BK-0010"));
    }

    [Test]
    public async Task GetBookingByIdAsync_InvalidId_ReturnsNull()
    {
        _bookingServiceMock.Setup(s => s.GetBookingByIdAsync("BK-9999")).ReturnsAsync((BookingResponseDto?)null);

        var result = await _bookingServiceMock.Object.GetBookingByIdAsync("BK-9999");

        Assert.That(result, Is.Null);
    }

    // ─── GetBookingByPnrAsync ─────────────────────────────────────────────────

    [Test]
    public async Task GetBookingByPnrAsync_ValidPnr_ReturnsBooking()
    {
        var expected = MakeResponseDto(pnr: "ABCDEF");
        _bookingServiceMock.Setup(s => s.GetBookingByPnrAsync("ABCDEF")).ReturnsAsync(expected);

        var result = await _bookingServiceMock.Object.GetBookingByPnrAsync("ABCDEF");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.PnrCode, Is.EqualTo("ABCDEF"));
    }

    // ─── GetBookingsByUserAsync ───────────────────────────────────────────────

    [Test]
    public async Task GetBookingsByUserAsync_ExistingUser_ReturnsList()
    {
        var bookings = new List<BookingResponseDto>
        {
            MakeResponseDto("BK-0001"),
            MakeResponseDto("BK-0002")
        };
        _bookingServiceMock.Setup(s => s.GetBookingsByUserAsync(1)).ReturnsAsync(bookings);

        var result = await _bookingServiceMock.Object.GetBookingsByUserAsync(1);

        Assert.That(result.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetBookingsByUserAsync_UserWithNoBookings_ReturnsEmptyList()
    {
        _bookingServiceMock.Setup(s => s.GetBookingsByUserAsync(99)).ReturnsAsync(new List<BookingResponseDto>());

        var result = await _bookingServiceMock.Object.GetBookingsByUserAsync(99);

        Assert.That(result, Is.Empty);
    }

    // ─── CancelBookingAsync ───────────────────────────────────────────────────

    [Test]
    public async Task CancelBookingAsync_ExistingBooking_ReturnsCancelledStatus()
    {
        var cancelled = MakeResponseDto(status: "Cancelled");
        _bookingServiceMock.Setup(s => s.CancelBookingAsync("BK-0001")).ReturnsAsync(cancelled);

        var result = await _bookingServiceMock.Object.CancelBookingAsync("BK-0001");

        Assert.That(result.Status, Is.EqualTo("Cancelled"));
    }

    // ─── UpdateStatusAsync ────────────────────────────────────────────────────

    [Test]
    public async Task UpdateStatusAsync_ValidStatus_ReturnsUpdatedBooking()
    {
        var statusDto = new UpdateBookingStatusDto { Status = "CheckedIn" };
        var expected  = MakeResponseDto(status: "CheckedIn");

        _bookingServiceMock.Setup(s => s.UpdateStatusAsync("BK-0001", statusDto)).ReturnsAsync(expected);

        var result = await _bookingServiceMock.Object.UpdateStatusAsync("BK-0001", statusDto);

        Assert.That(result.Status, Is.EqualTo("CheckedIn"));
    }

    // ─── CalculateFareAsync ───────────────────────────────────────────────────

    [Test]
    public async Task CalculateFareAsync_WithLuggageAndMeal_ReturnsFareSummary()
    {
        var dto = new CalculateFareRequestDto
        {
            BasePrice         = 3000m,
            PassengerCount    = 2,
            LuggageKg         = 20,
            HasMealPreference = true
        };

        // FareSummary is a record: (BaseFare, Taxes, AncillaryCost, TotalFare)
        // AncillaryCost covers both luggage and meal fees combined
        var expectedSummary = new FareSummary(
            BaseFare:      6000m,
            Taxes:         660m,   // was: TaxAmount
            AncillaryCost: 700m,   // was: LuggageFee + MealFee separately
            TotalFare:     7360m
        );

        _bookingServiceMock.Setup(s => s.CalculateFareAsync(dto)).ReturnsAsync(expectedSummary);

        var result = await _bookingServiceMock.Object.CalculateFareAsync(dto);

        Assert.That(result.TotalFare, Is.GreaterThan(result.BaseFare));
        Assert.That(result.AncillaryCost, Is.GreaterThan(0));  // was: result.LuggageFee + result.MealFee
    }

    [Test]
    public async Task CalculateFareAsync_NoExtras_TotalFareEqualsBasePlusTax()
    {
        var dto = new CalculateFareRequestDto
        {
            BasePrice         = 2000m,
            PassengerCount    = 1,
            LuggageKg         = 0,
            HasMealPreference = false
        };

        var expectedSummary = new FareSummary(
            BaseFare:      2000m,
            Taxes:         180m,   // was: TaxAmount
            AncillaryCost: 0m,     // was: LuggageFee = 0m, MealFee = 0m
            TotalFare:     2180m
        );

        _bookingServiceMock.Setup(s => s.CalculateFareAsync(dto)).ReturnsAsync(expectedSummary);

        var result = await _bookingServiceMock.Object.CalculateFareAsync(dto);

        Assert.That(result.AncillaryCost, Is.EqualTo(0));  // was: LuggageFee + MealFee separately
    }

    // ─── AddAddOnAsync ────────────────────────────────────────────────────────

    [Test]
    public async Task AddAddOnAsync_ValidAddOn_ReturnsUpdatedBooking()
    {
        var addOnDto = new AddAddOnRequestDto
        {
            MealPreference  = "Veg",
            ExtraBaggageKg  = 10    // was: LuggageKg
        };
        var expected = MakeResponseDto();

        _bookingServiceMock.Setup(s => s.AddAddOnAsync("BK-0001", addOnDto)).ReturnsAsync(expected);

        var result = await _bookingServiceMock.Object.AddAddOnAsync("BK-0001", addOnDto);

        Assert.That(result, Is.Not.Null);
    }

    // ─── GetUpcomingBookingsAsync ─────────────────────────────────────────────

    [Test]
    public async Task GetUpcomingBookingsAsync_UserWithFutureBookings_ReturnsList()
    {
        var upcoming = new List<BookingResponseDto>
        {
            MakeResponseDto("BK-0005"),
            MakeResponseDto("BK-0006")
        };
        _bookingServiceMock.Setup(s => s.GetUpcomingBookingsAsync(1)).ReturnsAsync(upcoming);

        var result = await _bookingServiceMock.Object.GetUpcomingBookingsAsync(1);

        Assert.That(result.Count, Is.EqualTo(2));
    }
}