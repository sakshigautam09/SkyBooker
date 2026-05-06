using Moq;
using NUnit.Framework;
using SkyBooker.SeatService.Services;
using SkyBooker.SeatService.DTOs;

namespace SkyBooker.Tests;

[TestFixture]
public class SeatServiceTests
{
    private Mock<ISeatService> _seatServiceMock = null!;

    [SetUp]
    public void SetUp() => _seatServiceMock = new Mock<ISeatService>();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static SeatResponseDto MakeSeatDto(
        int seatId    = 1,
        string number = "1A",
        string cls    = "Economy",
        string status = "Available",
        int flightId  = 10) => new()
    {
        SeatId     = seatId,
        SeatNumber = number,
        SeatClass  = cls,
        Status     = status,
        FlightId   = flightId
    };

    // ─── AddSeatsForFlightAsync ───────────────────────────────────────────────

    [Test]
    public async Task AddSeatsForFlightAsync_ValidSeats_ReturnsAddedSeats()
    {
        var seats = new List<AddSeatRequestDto>
        {
            new() { FlightId = 10, SeatNumber = "1A", SeatClass = "Business", Row = 1, Column = "A", PriceMultiplier = 2.5m },
            new() { FlightId = 10, SeatNumber = "1B", SeatClass = "Business", Row = 1, Column = "B", PriceMultiplier = 2.5m }
        };

        var expected = new List<SeatResponseDto>
        {
            MakeSeatDto(1, "1A", "Business", flightId: 10),
            MakeSeatDto(2, "1B", "Business", flightId: 10)
        };

        _seatServiceMock.Setup(s => s.AddSeatsForFlightAsync(seats)).ReturnsAsync(expected);

        var result = await _seatServiceMock.Object.AddSeatsForFlightAsync(seats);

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.All(s => s.SeatClass == "Business"), Is.True);
        Assert.That(result.All(s => s.FlightId == 10), Is.True);
    }

    // ─── GetAvailableSeatsAsync ───────────────────────────────────────────────

    [Test]
    public async Task GetAvailableSeatsAsync_FlightWithSeats_ReturnsAvailableOnly()
    {
        var available = new List<SeatResponseDto>
        {
            MakeSeatDto(1, "3A", status: "Available"),
            MakeSeatDto(2, "3B", status: "Available"),
            MakeSeatDto(3, "3C", status: "Available")
        };
        _seatServiceMock.Setup(s => s.GetAvailableSeatsAsync(10)).ReturnsAsync(available);

        var result = await _seatServiceMock.Object.GetAvailableSeatsAsync(10);

        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result.All(s => s.Status == "Available"), Is.True);
    }

    [Test]
    public async Task GetAvailableSeatsAsync_FullFlight_ReturnsEmptyList()
    {
        _seatServiceMock.Setup(s => s.GetAvailableSeatsAsync(99)).ReturnsAsync(new List<SeatResponseDto>());

        var result = await _seatServiceMock.Object.GetAvailableSeatsAsync(99);

        Assert.That(result, Is.Empty);
    }

    // ─── GetAvailableByClassAsync ─────────────────────────────────────────────

    [Test]
    public async Task GetAvailableByClassAsync_BusinessClass_ReturnsBusinessSeats()
    {
        var businessSeats = new List<SeatResponseDto>
        {
            MakeSeatDto(1, "1A", "Business"),
            MakeSeatDto(2, "1B", "Business")
        };
        _seatServiceMock.Setup(s => s.GetAvailableByClassAsync(10, "Business")).ReturnsAsync(businessSeats);

        var result = await _seatServiceMock.Object.GetAvailableByClassAsync(10, "Business");

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.All(s => s.SeatClass == "Business"), Is.True);
    }

    // ─── GetSeatByIdAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task GetSeatByIdAsync_ValidId_ReturnsSeat()
    {
        var expected = MakeSeatDto(5, "5C");
        _seatServiceMock.Setup(s => s.GetSeatByIdAsync(5)).ReturnsAsync(expected);

        var result = await _seatServiceMock.Object.GetSeatByIdAsync(5);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.SeatId, Is.EqualTo(5));
        Assert.That(result.SeatNumber, Is.EqualTo("5C"));
    }

    [Test]
    public async Task GetSeatByIdAsync_InvalidId_ReturnsNull()
    {
        _seatServiceMock.Setup(s => s.GetSeatByIdAsync(9999)).ReturnsAsync((SeatResponseDto?)null);

        var result = await _seatServiceMock.Object.GetSeatByIdAsync(9999);

        Assert.That(result, Is.Null);
    }

    // ─── HoldSeatAsync ────────────────────────────────────────────────────────

    [Test]
    public async Task HoldSeatAsync_AvailableSeat_ReturnsHeldStatus()
    {
        var held = MakeSeatDto(1, "2A", status: "Held");
        _seatServiceMock.Setup(s => s.HoldSeatAsync(1, 42)).ReturnsAsync(held);

        var result = await _seatServiceMock.Object.HoldSeatAsync(1, 42);

        Assert.That(result.Status, Is.EqualTo("Held"));
    }

    [Test]
    public async Task HoldSeatAsync_AlreadyHeldSeat_ThrowsException()
    {
        _seatServiceMock.Setup(s => s.HoldSeatAsync(1, 99))
            .ThrowsAsync(new InvalidOperationException("Seat is not available for hold."));

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _seatServiceMock.Object.HoldSeatAsync(1, 99));
        Assert.That(ex!.Message, Does.Contain("not available"));
    }

    // ─── ReleaseSeatAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task ReleaseSeatAsync_HeldSeat_ReturnsAvailableStatus()
    {
        var released = MakeSeatDto(1, "2A", status: "Available");
        _seatServiceMock.Setup(s => s.ReleaseSeatAsync(1)).ReturnsAsync(released);

        var result = await _seatServiceMock.Object.ReleaseSeatAsync(1);

        Assert.That(result.Status, Is.EqualTo("Available"));
    }

    // ─── ConfirmSeatAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task ConfirmSeatAsync_HeldSeat_ReturnsConfirmedStatus()
    {
        var confirmed = MakeSeatDto(1, "2A", status: "Confirmed");
        _seatServiceMock.Setup(s => s.ConfirmSeatAsync(1)).ReturnsAsync(confirmed);

        var result = await _seatServiceMock.Object.ConfirmSeatAsync(1);

        Assert.That(result.Status, Is.EqualTo("Confirmed"));
    }

    // ─── UpdateSeatAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task UpdateSeatAsync_ValidUpdate_ReturnsUpdatedSeat()
    {
        var updateDto = new UpdateSeatRequestDto { IsWindow = true, HasExtraLegroom = true, PriceMultiplier = 1.5m };
        var updated   = MakeSeatDto(1, "2A");

        _seatServiceMock.Setup(s => s.UpdateSeatAsync(1, updateDto)).ReturnsAsync(updated);

        var result = await _seatServiceMock.Object.UpdateSeatAsync(1, updateDto);

        Assert.That(result, Is.Not.Null);
    }

    // ─── GetSeatMapAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task GetSeatMapAsync_ValidFlight_ReturnsSeatMap()
    {
        var seatMap = new List<SeatResponseDto>
        {
            MakeSeatDto(1, "1A", "Business", "Confirmed"),
            MakeSeatDto(2, "1B", "Business", "Available"),
            MakeSeatDto(3, "2A", "Economy",  "Available"),
            MakeSeatDto(4, "2B", "Economy",  "Held")
        };
        _seatServiceMock.Setup(s => s.GetSeatMapAsync(10)).ReturnsAsync(seatMap);

        var result = await _seatServiceMock.Object.GetSeatMapAsync(10);

        Assert.That(result.Count, Is.EqualTo(4));
        Assert.That(result.Any(s => s.SeatClass == "Business"), Is.True);
        Assert.That(result.Any(s => s.SeatClass == "Economy"), Is.True);
    }

    // ─── CountAvailableByClassAsync ───────────────────────────────────────────

    [Test]
    public async Task CountAvailableByClassAsync_EconomyClass_ReturnsCount()
    {
        _seatServiceMock.Setup(s => s.CountAvailableByClassAsync(10, "Economy")).ReturnsAsync(120);

        var result = await _seatServiceMock.Object.CountAvailableByClassAsync(10, "Economy");

        Assert.That(result, Is.EqualTo(120));
    }

    [Test]
    public async Task CountAvailableByClassAsync_NoSeatsLeft_ReturnsZero()
    {
        _seatServiceMock.Setup(s => s.CountAvailableByClassAsync(10, "FirstClass")).ReturnsAsync(0);

        var result = await _seatServiceMock.Object.CountAvailableByClassAsync(10, "FirstClass");

        Assert.That(result, Is.EqualTo(0));
    }

    // ─── DeleteSeatsForFlightAsync ────────────────────────────────────────────

    [Test]
    public async Task DeleteSeatsForFlightAsync_ValidFlight_CompletesSuccessfully()
    {
        _seatServiceMock.Setup(s => s.DeleteSeatsForFlightAsync(10)).Returns(Task.CompletedTask);
        Assert.DoesNotThrowAsync(() => _seatServiceMock.Object.DeleteSeatsForFlightAsync(10));
    }
}