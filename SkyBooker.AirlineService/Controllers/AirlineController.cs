using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyBooker.AirlineService.DTOs;
using SkyBooker.AirlineService.Services;

namespace SkyBooker.AirlineService.Controllers;

[ApiController]
public class AirlineController : ControllerBase
{
    private readonly IAirlineService _airlineService;

    public AirlineController(IAirlineService airlineService)
    {
        _airlineService = airlineService;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AIRLINE ENDPOINTS  —  [Route("api/airlines")]
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Create a new airline (Admin only)</summary>
    [HttpPost("api/airlines")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAirline([FromBody] CreateAirlineRequestDto dto)
    {
        try
        {
            var result = await _airlineService.CreateAirlineAsync(dto);
            return CreatedAtAction(nameof(GetAirlineById),
                new { airlineId = result.AirlineId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Get airline by ID</summary>
    [HttpGet("api/airlines/{airlineId:int}")]
    [Authorize]
    public async Task<IActionResult> GetAirlineById(int airlineId)
    {
        try
        {
            var result = await _airlineService.GetAirlineByIdAsync(airlineId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Get airline by IATA code</summary>
    [HttpGet("api/airlines/iata/{iataCode}")]
    [Authorize]
    public async Task<IActionResult> GetAirlineByIata(string iataCode)
    {
        try
        {
            var result = await _airlineService.GetAirlineByIataAsync(iataCode);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Get all airlines</summary>
    [HttpGet("api/airlines")]
    [Authorize]
    public async Task<IActionResult> GetAllAirlines()
    {
        var result = await _airlineService.GetAllAirlinesAsync();
        return Ok(result);
    }

    /// <summary>Update airline details (Admin only)</summary>
    [HttpPut("api/airlines/{airlineId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAirline(
        int airlineId, [FromBody] UpdateAirlineRequestDto dto)
    {
        try
        {
            var result = await _airlineService.UpdateAirlineAsync(airlineId, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Deactivate an airline (Admin only)</summary>
    [HttpPut("api/airlines/{airlineId:int}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateAirline(int airlineId)
    {
        try
        {
            var result = await _airlineService.DeactivateAirlineAsync(airlineId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AIRPORT ENDPOINTS  —  [Route("api/airports")]
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Create a new airport (Admin only)</summary>
    [HttpPost("api/airports")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAirport([FromBody] CreateAirportRequestDto dto)
    {
        try
        {
            var result = await _airlineService.CreateAirportAsync(dto);
            return CreatedAtAction(nameof(GetAirportById),
                new { airportId = result.AirportId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Get airport by ID</summary>
    [HttpGet("api/airports/{airportId:int}")]
    [Authorize]
    public async Task<IActionResult> GetAirportById(int airportId)
    {
        try
        {
            var result = await _airlineService.GetAirportByIdAsync(airportId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Get airport by IATA code</summary>
    [HttpGet("api/airports/iata/{iataCode}")]
    [Authorize]
    public async Task<IActionResult> GetAirportByIata(string iataCode)
    {
        try
        {
            var result = await _airlineService.GetAirportByIataAsync(iataCode);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Autocomplete airport search by name, IATA code, or city (EF.Functions.Like)</summary>
    [HttpGet("api/airports/search")]
    [Authorize]
    public async Task<IActionResult> SearchAirports([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Search query 'q' is required." });

        var result = await _airlineService.SearchAirportsAsync(q);
        return Ok(result);
    }

    /// <summary>Get airports by city</summary>
    [HttpGet("api/airports/city/{city}")]
    [Authorize]
    public async Task<IActionResult> GetAirportsByCity(string city)
    {
        var result = await _airlineService.GetAirportsByCityAsync(city);
        return Ok(result);
    }

    /// <summary>Get airports by country</summary>
    [HttpGet("api/airports/country/{country}")]
    [Authorize]
    public async Task<IActionResult> GetAirportsByCountry(string country)
    {
        var result = await _airlineService.GetAirportsByCountryAsync(country);
        return Ok(result);
    }

    /// <summary>Get all airports</summary>
    [HttpGet("api/airports")]
    [Authorize]
    public async Task<IActionResult> GetAllAirports()
    {
        var result = await _airlineService.GetAllAirportsAsync();
        return Ok(result);
    }

    /// <summary>Update airport details (Admin only)</summary>
    [HttpPut("api/airports/{airportId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAirport(
        int airportId, [FromBody] UpdateAirportRequestDto dto)
    {
        try
        {
            var result = await _airlineService.UpdateAirportAsync(airportId, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
