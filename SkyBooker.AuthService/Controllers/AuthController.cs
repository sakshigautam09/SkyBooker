using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyBooker.AuthService.DTOs;
using SkyBooker.AuthService.Enums;
using SkyBooker.AuthService.Services;

namespace SkyBooker.AuthService.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequestDto> _validator;

    public AuthController(IAuthService authService, IValidator<RegisterRequestDto> validator)
    {
        _authService = authService;
        _validator = validator;
    }

    /// <summary>Register a new user</summary>
    /// <remarks>
    /// Role must be **Passenger** or **AirlineStaff**. Admin role cannot be self-assigned.
    ///
    /// Password rules:
    /// - Minimum 8 characters
    /// - At least 1 uppercase letter (A-Z)
    /// - At least 1 lowercase letter (a-z)
    /// - At least 1 digit (0-9)
    /// - At least 1 special character (@$!%*?&amp;)
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        try
        {
            var result = await _authService.RegisterAsync(dto);
            return CreatedAtAction(nameof(Register), new { id = result.UserId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Login with email and password</summary>
    /// <remarks>Returns an accessToken (JWT) and a refreshToken. Use the accessToken in the Authorize button above.</remarks>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { message = "Email and password are required." });

        try
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>Logout and revoke the refresh token</summary>
    /// <remarks>Requires Bearer token in Authorize. Revokes the provided refresh token.</remarks>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        await _authService.LogoutAsync(userId.Value, dto.RefreshToken);
        return NoContent();
    }

    /// <summary>Refresh the access token using a refresh token</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>Get the currently logged-in user's profile</summary>
    /// <remarks>Click the Authorize button at the top and enter: **Bearer {accessToken}**</remarks>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        try
        {
            var profile = await _authService.GetUserByIdAsync(userId.Value);
            return Ok(profile);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Update the currently logged-in user's profile</summary>
    /// <remarks>Only provide the fields you want to update. Other fields remain unchanged.</remarks>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        try
        {
            var updated = await _authService.UpdateProfileAsync(userId.Value, dto);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Change password for the currently logged-in user</summary>
    /// <remarks>
    /// New password rules:
    /// - Minimum 8 characters
    /// - At least 1 uppercase, 1 lowercase, 1 digit, 1 special character (@$!%*?&amp;)
    /// </remarks>
    [HttpPut("password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        try
        {
            await _authService.ChangePasswordAsync(userId.Value, dto);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Deactivate the currently logged-in user's account</summary>
    /// <remarks>This will also revoke all refresh tokens for the user.</remarks>
    [HttpDelete("deactivate")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeactivateAccount()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        try
        {
            await _authService.DeactivateAccountAsync(userId.Value);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Get all users — Admin only</summary>
    /// <remarks>
    /// Requires Admin role. Optionally filter by role: **Passenger**, **AirlineStaff**, **Admin**
    /// </remarks>
    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IEnumerable<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllUsers([FromQuery] string? role = null)
    {
        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, ignoreCase: true, out var userRole))
        {
            var filtered = await _authService.GetAllUsersByRoleAsync(userRole);
            return Ok(filtered);
        }

        var users = await _authService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>Validate a JWT access token</summary>
    /// <remarks>Returns the userId if the token is valid.</remarks>
    [HttpPost("validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ValidateToken([FromBody] string token)
    {
        try
        {
            var userId = await _authService.ValidateTokenAsync(token);
            return Ok(new { userId, valid = true });
        }
        catch
        {
            return Unauthorized(new { valid = false, message = "Invalid or expired token." });
        }
    }

    // ─── Helper ──────────────────────────────────────────────────────────────

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
