using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyBooker.FlightService.DTOs;
using SkyBooker.FlightService.Services;

namespace SkyBooker.FlightService.Controllers;

[ApiController]
[Route("api/flights")]
public class FlightController : ControllerBase
{
    private readonly IFlightService _flightService;

    public FlightController(IFlightService flightService)
    {
        _flightService = flightService;
    }

    /// <summary>Add a new flight schedule (Airline Staff / Admin only)</summary>
    [HttpPost]
    [Authorize(Roles = "AirlineStaff,Admin")]
    public async Task<IActionResult> AddFlight([FromBody] AddFlightRequestDto dto)
    {
        try
        {
            var result = await _flightService.AddFlightAsync(dto);
            return CreatedAtAction(nameof(GetById), new { flightId = result.FlightId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Get flight by ID</summary>
    [HttpGet("{flightId:int}")]
    public async Task<IActionResult> GetById(int flightId)
    {
        var result = await _flightService.GetFlightByIdAsync(flightId);
        return result == null ? NotFound(new { message = "Flight not found." }) : Ok(result);
    }

    /// <summary>Get flight by flight number</summary>
    [HttpGet("number/{flightNumber}")]
    public async Task<IActionResult> GetByNumber(string flightNumber)
    {
        var result = await _flightService.GetFlightByNumberAsync(flightNumber);
        return result == null ? NotFound(new { message = "Flight not found." }) : Ok(result);
    }

    /// <summary>Search one-way available flights by origin, destination, and date</summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string origin,
        [FromQuery] string destination,
        [FromQuery] DateTime date)
    {
        var results = await _flightService.SearchFlightsAsync(origin, destination, date);
        return Ok(results);
    }

    /// <summary>Search round-trip flights — returns outbound and return lists</summary>
    [HttpGet("search/roundtrip")]
    public async Task<IActionResult> SearchRoundTrip(
        [FromQuery] string origin,
        [FromQuery] string destination,
        [FromQuery] DateTime departureDate,
        [FromQuery] DateTime returnDate)
    {
        var results = await _flightService.SearchRoundTripAsync(
            origin, destination, departureDate, returnDate);
        return Ok(results);
    }

    /// <summary>Get all flights for a specific airline</summary>
    [HttpGet("airline/{airlineId:int}")]
    public async Task<IActionResult> GetByAirline(int airlineId)
    {
        var results = await _flightService.GetFlightsByAirlineAsync(airlineId);
        return Ok(results);
    }

    /// <summary>Update flight schedule details (Airline Staff / Admin only)</summary>
    [HttpPut("{flightId:int}")]
    [Authorize(Roles = "AirlineStaff,Admin")]
    public async Task<IActionResult> UpdateFlight(int flightId, [FromBody] UpdateFlightRequestDto dto)
    {
        try
        {
            var result = await _flightService.UpdateFlightAsync(flightId, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Update flight status — Scheduled, OnTime, Delayed, Cancelled, Departed, Arrived</summary>
    [HttpPut("{flightId:int}/status")]
    [Authorize(Roles = "AirlineStaff,Admin")]
    public async Task<IActionResult> UpdateStatus(int flightId, [FromBody] UpdateFlightStatusDto dto)
    {
        try
        {
            var result = await _flightService.UpdateStatusAsync(flightId, dto);
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

    /// <summary>Decrement available seats — called internally by Booking Service</summary>
    [HttpPut("{flightId:int}/decrement-seats")]
    [Authorize]
    public async Task<IActionResult> DecrementSeats(int flightId)
    {
        try
        {
            await _flightService.DecrementSeatsAsync(flightId);
            return Ok(new { message = "Seat decremented successfully." });
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

    /// <summary>Increment available seats — called internally when booking is cancelled</summary>
    [HttpPut("{flightId:int}/increment-seats")]
    [Authorize]
    public async Task<IActionResult> IncrementSeats(int flightId)
    {
        try
        {
            await _flightService.IncrementSeatsAsync(flightId);
            return Ok(new { message = "Seat incremented successfully." });
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

    /// <summary>Delete a flight (Admin only)</summary>
    [HttpDelete("{flightId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteFlight(int flightId)
    {
        try
        {
            await _flightService.DeleteFlightAsync(flightId);
            return Ok(new { message = "Flight deleted successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
