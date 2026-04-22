using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyBooker.PassengerService.DTOs;
using SkyBooker.PassengerService.Services;

namespace SkyBooker.PassengerService.Controllers;

[ApiController]
[Route("api/passengers")]
public class PassengerController : ControllerBase
{
    private readonly IPassengerService _passengerService;

    public PassengerController(IPassengerService passengerService)
    {
        _passengerService = passengerService;
    }

    /// <summary>Add a passenger to a booking — validates passport expiry and age eligibility</summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddPassenger([FromBody] AddPassengerRequestDto dto)
    {
        try
        {
            var result = await _passengerService.AddPassengerAsync(dto);
            return CreatedAtAction(nameof(GetById), new { passengerId = result.PassengerId }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Get passenger by ID</summary>
    [HttpGet("{passengerId:int}")]
    [Authorize]
    public async Task<IActionResult> GetById(int passengerId)
    {
        var result = await _passengerService.GetPassengerByIdAsync(passengerId);
        return result == null
            ? NotFound(new { message = "Passenger not found." })
            : Ok(result);
    }

    /// <summary>Get all passengers for a booking</summary>
    [HttpGet("booking/{bookingId}")]
    [Authorize]
    public async Task<IActionResult> GetByBooking(string bookingId)
    {
        var result = await _passengerService.GetPassengersByBookingAsync(bookingId);
        return Ok(result);
    }

    /// <summary>Get passenger count for a booking</summary>
    [HttpGet("booking/{bookingId}/count")]
    [Authorize]
    public async Task<IActionResult> GetCount(string bookingId)
    {
        var count = await _passengerService.GetPassengerCountAsync(bookingId);
        return Ok(new { bookingId, passengerCount = count });
    }

    /// <summary>Get passenger by passport number</summary>
    [HttpGet("passport/{passportNumber}")]
    [Authorize]
    public async Task<IActionResult> GetByPassport(string passportNumber)
    {
        var result = await _passengerService.GetByPassportNumberAsync(passportNumber);
        return result == null
            ? NotFound(new { message = "No passenger found with that passport number." })
            : Ok(result);
    }

    /// <summary>Get passenger by ticket number</summary>
    [HttpGet("ticket/{ticketNumber}")]
    public async Task<IActionResult> GetByTicket(string ticketNumber)
    {
        var result = await _passengerService.GetByTicketNumberAsync(ticketNumber);
        return result == null
            ? NotFound(new { message = "No passenger found with that ticket number." })
            : Ok(result);
    }

    /// <summary>Update passenger details</summary>
    [HttpPut("{passengerId:int}")]
    [Authorize]
    public async Task<IActionResult> UpdatePassenger(
        int passengerId, [FromBody] UpdatePassengerRequestDto dto)
    {
        try
        {
            var result = await _passengerService.UpdatePassengerAsync(passengerId, dto);
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

    /// <summary>Assign seat to passenger after web check-in</summary>
    [HttpPut("{passengerId:int}/assign-seat")]
    [Authorize]
    public async Task<IActionResult> AssignSeat(
        int passengerId, [FromBody] AssignSeatRequestDto dto)
    {
        try
        {
            var result = await _passengerService.AssignSeatAsync(passengerId, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Delete a passenger by ID</summary>
    [HttpDelete("{passengerId:int}")]
    [Authorize]
    public async Task<IActionResult> DeletePassenger(int passengerId)
    {
        try
        {
            await _passengerService.DeletePassengerAsync(passengerId);
            return Ok(new { message = "Passenger deleted successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Delete all passengers for a booking — called on booking cancellation</summary>
    [HttpDelete("booking/{bookingId}")]
    [Authorize]
    public async Task<IActionResult> DeleteByBooking(string bookingId)
    {
        await _passengerService.DeletePassengersByBookingAsync(bookingId);
        return Ok(new { message = $"All passengers for booking {bookingId} deleted." });
    }
}
