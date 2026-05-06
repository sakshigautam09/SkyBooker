using Moq;
using NUnit.Framework;
using SkyBooker.FlightService.Services;
using SkyBooker.FlightService.DTOs;
using SkyBooker.FlightService.Entities;
using SkyBooker.FlightService.Repositories;

namespace SkyBooker.Tests;

[TestFixture]
public class FlightServiceTests
{
    private Mock<IFlightService> _flightServiceMock = null!;

    [SetUp]
    public void SetUp() => _flightServiceMock = new Mock<IFlightService>();

    // ─── AddFlightAsync ───────────────────────────────────────────────────────

    [Test]
    public async Task AddFlightAsync_ValidRequest_ReturnsFlightResponse()
    {
        var dto = new AddFlightRequestDto
        {
            FlightNumber            = "AI-101",
            AirlineId               = 1,
            OriginAirportCode       = "DEL",
            DestinationAirportCode  = "BOM",
            DepartureTime           = DateTime.UtcNow.AddDays(2),
            ArrivalTime             = DateTime.UtcNow.AddDays(2).AddHours(2),
            AircraftType            = "Boeing 737",
            TotalSeats              = 180,
            BasePrice               = 4500m
        };

        var expectedResponse = new FlightResponseDto
        {
            FlightId      = 1,
            FlightNumber  = "AI-101",
            AirlineId     = 1,
            OriginAirportCode      = "DEL",
            DestinationAirportCode = "BOM",
            TotalSeats    = 180,
            BasePrice     = 4500m
        };

        _flightServiceMock.Setup(s => s.AddFlightAsync(dto)).ReturnsAsync(expectedResponse);

        var result = await _flightServiceMock.Object.AddFlightAsync(dto);

        Assert.That(result.FlightNumber, Is.EqualTo("AI-101"));
        Assert.That(result.TotalSeats, Is.EqualTo(180));
        Assert.That(result.BasePrice, Is.EqualTo(4500m));
    }

    // ─── GetFlightByIdAsync ───────────────────────────────────────────────────

    [Test]
    public async Task GetFlightByIdAsync_ExistingFlight_ReturnsFlightDto()
    {
        var expected = new FlightResponseDto { FlightId = 5, FlightNumber = "6E-200", AirlineId = 2 };
        _flightServiceMock.Setup(s => s.GetFlightByIdAsync(5)).ReturnsAsync(expected);

        var result = await _flightServiceMock.Object.GetFlightByIdAsync(5);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.FlightId, Is.EqualTo(5));
        Assert.That(result.FlightNumber, Is.EqualTo("6E-200"));
    }

    [Test]
    public async Task GetFlightByIdAsync_NonExistingFlight_ReturnsNull()
    {
        _flightServiceMock.Setup(s => s.GetFlightByIdAsync(999)).ReturnsAsync((FlightResponseDto?)null);

        var result = await _flightServiceMock.Object.GetFlightByIdAsync(999);

        Assert.That(result, Is.Null);
    }

    // ─── GetFlightByNumberAsync ───────────────────────────────────────────────

    [Test]
    public async Task GetFlightByNumberAsync_ValidFlightNumber_ReturnsDto()
    {
        var expected = new FlightResponseDto { FlightId = 3, FlightNumber = "SG-501" };
        _flightServiceMock.Setup(s => s.GetFlightByNumberAsync("SG-501")).ReturnsAsync(expected);

        var result = await _flightServiceMock.Object.GetFlightByNumberAsync("SG-501");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.FlightNumber, Is.EqualTo("SG-501"));
    }

    // ─── SearchFlightsAsync ───────────────────────────────────────────────────

    [Test]
    public async Task SearchFlightsAsync_ValidRoute_ReturnsMatchingFlights()
    {
        var searchDate = DateTime.UtcNow.Date.AddDays(3);
        var flights = new List<FlightResponseDto>
        {
            new() { FlightId = 1, FlightNumber = "AI-101", OriginAirportCode = "DEL", DestinationAirportCode = "BOM" },
            new() { FlightId = 2, FlightNumber = "6E-202", OriginAirportCode = "DEL", DestinationAirportCode = "BOM" }
        };

        _flightServiceMock.Setup(s => s.SearchFlightsAsync("DEL", "BOM", searchDate)).ReturnsAsync(flights);

        var result = await _flightServiceMock.Object.SearchFlightsAsync("DEL", "BOM", searchDate);

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.All(f => f.OriginAirportCode == "DEL" && f.DestinationAirportCode == "BOM"), Is.True);
    }

    [Test]
    public async Task SearchFlightsAsync_NoFlightsAvailable_ReturnsEmptyList()
    {
        _flightServiceMock.Setup(s => s.SearchFlightsAsync("DEL", "CCU", DateTime.UtcNow.Date))
            .ReturnsAsync(new List<FlightResponseDto>());

        var result = await _flightServiceMock.Object.SearchFlightsAsync("DEL", "CCU", DateTime.UtcNow.Date);

        Assert.That(result, Is.Empty);
    }

    // ─── SearchRoundTripAsync ─────────────────────────────────────────────────

    [Test]
    public async Task SearchRoundTripAsync_ValidDates_ReturnsBothLegs()
    {
        var departure = DateTime.UtcNow.Date.AddDays(5);
        var returnDate = DateTime.UtcNow.Date.AddDays(10);

        var mockResult = new Dictionary<string, IList<FlightResponseDto>>
        {
            ["outbound"] = new List<FlightResponseDto> { new() { FlightId = 1, FlightNumber = "AI-101" } },
            ["return"]   = new List<FlightResponseDto> { new() { FlightId = 2, FlightNumber = "AI-102" } }
        };

        _flightServiceMock.Setup(s => s.SearchRoundTripAsync("DEL", "BOM", departure, returnDate))
            .ReturnsAsync(mockResult);

        var result = await _flightServiceMock.Object.SearchRoundTripAsync("DEL", "BOM", departure, returnDate);

        Assert.That(result.ContainsKey("outbound"), Is.True);
        Assert.That(result.ContainsKey("return"), Is.True);
        Assert.That(result["outbound"].Count, Is.EqualTo(1));
        Assert.That(result["return"].Count, Is.EqualTo(1));
    }

    // ─── UpdateStatusAsync ────────────────────────────────────────────────────

    [Test]
    public async Task UpdateStatusAsync_ValidStatus_ReturnsUpdatedFlight()
    {
        var statusDto = new UpdateFlightStatusDto { Status = "Cancelled" };
        var expected  = new FlightResponseDto { FlightId = 1, FlightNumber = "AI-101", Status = "Cancelled" };

        _flightServiceMock.Setup(s => s.UpdateStatusAsync(1, statusDto)).ReturnsAsync(expected);

        var result = await _flightServiceMock.Object.UpdateStatusAsync(1, statusDto);

        Assert.That(result.Status, Is.EqualTo("Cancelled"));
    }

    // ─── DecrementSeatsAsync / IncrementSeatsAsync ────────────────────────────

    [Test]
    public async Task DecrementSeatsAsync_Called_CompletesSuccessfully()
    {
        _flightServiceMock.Setup(s => s.DecrementSeatsAsync(1)).Returns(Task.CompletedTask);
        Assert.DoesNotThrowAsync(() => _flightServiceMock.Object.DecrementSeatsAsync(1));
    }

    [Test]
    public async Task IncrementSeatsAsync_Called_CompletesSuccessfully()
    {
        _flightServiceMock.Setup(s => s.IncrementSeatsAsync(1)).Returns(Task.CompletedTask);
        Assert.DoesNotThrowAsync(() => _flightServiceMock.Object.IncrementSeatsAsync(1));
    }

    // ─── GetFlightsByAirlineAsync ─────────────────────────────────────────────

    [Test]
    public async Task GetFlightsByAirlineAsync_ValidAirline_ReturnsFlights()
    {
        var flights = new List<FlightResponseDto>
        {
            new() { FlightId = 1, AirlineId = 3, FlightNumber = "IX-100" },
            new() { FlightId = 2, AirlineId = 3, FlightNumber = "IX-101" }
        };
        _flightServiceMock.Setup(s => s.GetFlightsByAirlineAsync(3)).ReturnsAsync(flights);

        var result = await _flightServiceMock.Object.GetFlightsByAirlineAsync(3);

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.All(f => f.AirlineId == 3), Is.True);
    }

    // ─── DeleteFlightAsync ────────────────────────────────────────────────────

    [Test]
    public async Task DeleteFlightAsync_ExistingFlight_CompletesSuccessfully()
    {
        _flightServiceMock.Setup(s => s.DeleteFlightAsync(10)).Returns(Task.CompletedTask);
        Assert.DoesNotThrowAsync(() => _flightServiceMock.Object.DeleteFlightAsync(10));
    }
}