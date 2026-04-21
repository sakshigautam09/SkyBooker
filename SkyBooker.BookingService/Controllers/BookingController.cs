using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyBooker.BookingService.DTOs;
using SkyBooker.BookingService.Services;

namespace SkyBooker.BookingService.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>Create a new booking with EF Core transaction — returns PNR</summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequestDto dto)
    {
        try
        {
            var result = await _bookingService.CreateBookingAsync(dto);
            return CreatedAtAction(nameof(GetById), new { bookingId = result.BookingId }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Get booking by booking ID</summary>
    [HttpGet("{bookingId}")]
    [Authorize]
    public async Task<IActionResult> GetById(string bookingId)
    {
        var result = await _bookingService.GetBookingByIdAsync(bookingId);
        return result == null
            ? NotFound(new { message = "Booking not found." })
            : Ok(result);
    }

    /// <summary>Get booking by PNR code — no login required</summary>
    [HttpGet("pnr/{pnrCode}")]
    public async Task<IActionResult> GetByPnr(string pnrCode)
    {
        var result = await _bookingService.GetBookingByPnrAsync(pnrCode);
        return result == null
            ? NotFound(new { message = $"No booking found with PNR: {pnrCode}" })
            : Ok(result);
    }

    /// <summary>Get all bookings for a user</summary>
    [HttpGet("user/{userId:int}")]
    [Authorize]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var result = await _bookingService.GetBookingsByUserAsync(userId);
        return Ok(result);
    }

    /// <summary>Get upcoming confirmed bookings for a user</summary>
    [HttpGet("user/{userId:int}/upcoming")]
    [Authorize]
    public async Task<IActionResult> GetUpcoming(int userId)
    {
        var result = await _bookingService.GetUpcomingBookingsAsync(userId);
        return Ok(result);
    }

    /// <summary>Get all bookings for a flight (Airline Staff / Admin only)</summary>
    [HttpGet("flight/{flightId:int}")]
    [Authorize(Roles = "AirlineStaff,Admin")]
    public async Task<IActionResult> GetByFlight(int flightId)
    {
        var result = await _bookingService.GetBookingsByFlightAsync(flightId);
        return Ok(result);
    }

    /// <summary>Calculate fare — returns FareSummary with base, taxes, ancillary, total</summary>
    [HttpGet("calculate-fare")]
    public async Task<IActionResult> CalculateFare(
        [FromQuery] decimal basePrice,
        [FromQuery] int passengerCount = 1,
        [FromQuery] int luggageKg = 0,
        [FromQuery] bool hasMealPreference = false)
    {
        var result = await _bookingService.CalculateFareAsync(new CalculateFareRequestDto
        {
            BasePrice = basePrice,
            PassengerCount = passengerCount,
            LuggageKg = luggageKg,
            HasMealPreference = hasMealPreference
        });
        return Ok(result);
    }

    /// <summary>Cancel a booking — triggers SAGA compensation</summary>
    [HttpPut("{bookingId}/cancel")]
    [Authorize]
    public async Task<IActionResult> CancelBooking(string bookingId)
    {
        try
        {
            var result = await _bookingService.CancelBookingAsync(bookingId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Update booking status — called by Payment Service after webhook</summary>
    [HttpPut("{bookingId}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateStatus(
        string bookingId, [FromBody] UpdateBookingStatusDto dto)
    {
        try
        {
            var result = await _bookingService.UpdateStatusAsync(bookingId, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Add meal preference or extra baggage add-on to a booking</summary>
    [HttpPost("{bookingId}/addons")]
    [Authorize]
    public async Task<IActionResult> AddAddOn(
        string bookingId, [FromBody] AddAddOnRequestDto dto)
    {
        try
        {
            var result = await _bookingService.AddAddOnAsync(bookingId, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
