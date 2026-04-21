using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyBooker.SeatService.DTOs;
using SkyBooker.SeatService.Services;

namespace SkyBooker.SeatService.Controllers;

[ApiController]
[Route("api/seats")]
public class SeatController : ControllerBase
{
    private readonly ISeatService _seatService;

    public SeatController(ISeatService seatService)
    {
        _seatService = seatService;
    }

    /// <summary>Add seats for a flight (Airline Staff / Admin only)</summary>
    [HttpPost]
    [Authorize(Roles = "AirlineStaff,Admin")]
    public async Task<IActionResult> AddSeats([FromBody] IList<AddSeatRequestDto> dtos)
    {
        try
        {
            var result = await _seatService.AddSeatsForFlightAsync(dtos);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Get all available seats for a flight</summary>
    [HttpGet("{flightId:int}/available")]
    public async Task<IActionResult> GetAvailable(int flightId)
    {
        var result = await _seatService.GetAvailableSeatsAsync(flightId);
        return Ok(result);
    }

    /// <summary>Get available seats by class (Economy / Business / First)</summary>
    [HttpGet("{flightId:int}/available/{seatClass}")]
    public async Task<IActionResult> GetAvailableByClass(int flightId, string seatClass)
    {
        var result = await _seatService.GetAvailableByClassAsync(flightId, seatClass);
        return Ok(result);
    }

    /// <summary>Get full seat map for a flight — all seats with status</summary>
    [HttpGet("{flightId:int}/map")]
    public async Task<IActionResult> GetSeatMap(int flightId)
    {
        var result = await _seatService.GetSeatMapAsync(flightId);
        return Ok(result);
    }

    /// <summary>Get a single seat by ID</summary>
    [HttpGet("seat/{seatId:int}")]
    public async Task<IActionResult> GetById(int seatId)
    {
        var result = await _seatService.GetSeatByIdAsync(seatId);
        return result == null ? NotFound(new { message = "Seat not found." }) : Ok(result);
    }

    /// <summary>Count available seats by class for a flight</summary>
    [HttpGet("{flightId:int}/count/{seatClass}")]
    public async Task<IActionResult> CountByClass(int flightId, string seatClass)
    {
        var count = await _seatService.CountAvailableByClassAsync(flightId, seatClass);
        return Ok(new { flightId, seatClass, availableCount = count });
    }

    /// <summary>Hold a seat for a user — 15 minute TTL enforced by BackgroundService</summary>
    [HttpPut("seat/{seatId:int}/hold")]
    [Authorize]
    public async Task<IActionResult> HoldSeat(int seatId, [FromBody] HoldSeatRequestDto dto)
    {
        try
        {
            var result = await _seatService.HoldSeatAsync(seatId, dto.UserId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // 409 Conflict — seat taken (concurrency) or not available
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Release a held seat back to available</summary>
    [HttpPut("seat/{seatId:int}/release")]
    [Authorize]
    public async Task<IActionResult> ReleaseSeat(int seatId)
    {
        try
        {
            var result = await _seatService.ReleaseSeatAsync(seatId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Confirm a held seat after payment — called by Booking Service</summary>
    [HttpPut("seat/{seatId:int}/confirm")]
    [Authorize]
    public async Task<IActionResult> ConfirmSeat(int seatId)
    {
        try
        {
            var result = await _seatService.ConfirmSeatAsync(seatId);
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

    /// <summary>Update seat properties (Airline Staff / Admin only)</summary>
    [HttpPut("seat/{seatId:int}")]
    [Authorize(Roles = "AirlineStaff,Admin")]
    public async Task<IActionResult> UpdateSeat(int seatId, [FromBody] UpdateSeatRequestDto dto)
    {
        try
        {
            var result = await _seatService.UpdateSeatAsync(seatId, dto);
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

    /// <summary>Delete all seats for a flight (Admin only)</summary>
    [HttpDelete("{flightId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSeatsForFlight(int flightId)
    {
        await _seatService.DeleteSeatsForFlightAsync(flightId);
        return Ok(new { message = $"All seats for flight {flightId} deleted." });
    }
}
