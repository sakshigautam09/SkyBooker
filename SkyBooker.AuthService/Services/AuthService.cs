using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SkyBooker.AuthService.Data;
using SkyBooker.AuthService.DTOs;
using SkyBooker.AuthService.Entities;
using SkyBooker.AuthService.Enums;
using SkyBooker.AuthService.Repositories;

namespace SkyBooker.AuthService.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly AuthDbContext _context;

    public AuthService(IUserRepository userRepository, IConfiguration configuration, AuthDbContext context)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _context = context;
    }

    // ─── Register ────────────────────────────────────────────────────────────

    public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto dto)
    {
        if (await _userRepository.ExistsByEmailAsync(dto.Email))
            throw new InvalidOperationException("Email is already registered.");

        // Resolve role
        var role = UserRole.Passenger;
        if (!string.IsNullOrWhiteSpace(dto.Role))
        {
            if (!Enum.TryParse<UserRole>(dto.Role, ignoreCase: true, out var parsedRole))
                throw new InvalidOperationException("Invalid role. Allowed values: Passenger, AirlineStaff, Admin.");

            // Admin requires secret key validation
            if (parsedRole == UserRole.Admin)
            {
                var adminSecret = _configuration["AdminRegistration:SecretKey"];
                if (string.IsNullOrWhiteSpace(adminSecret) || dto.AdminSecretKey != adminSecret)
                    throw new UnauthorizedAccessException("Invalid AdminSecretKey. You are not authorized to register as Admin.");
            }

            role = parsedRole;
        }

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Phone = dto.Phone,
            PassportNumber = dto.PassportNumber,
            Nationality = dto.Nationality,
            Role = role,
            Provider = "local"
        };

        var created = await _userRepository.CreateAsync(user);

        return new RegisterResponseDto
        {
            UserId = created.UserId,
            FullName = created.FullName,
            Email = created.Email,
            Role = created.Role.ToString(),
            CreatedAt = created.CreatedAt
        };
    }

    // ─── Login ───────────────────────────────────────────────────────────────

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var user = await _userRepository.FindByEmailAsync(dto.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        if (user.PasswordHash is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var (accessToken, expiresAt) = GenerateJwtToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user.UserId);

        return BuildLoginResponse(user, accessToken, expiresAt, refreshToken);
    }

    // ─── Logout ──────────────────────────────────────────────────────────────

    public async Task LogoutAsync(int userId, string refreshToken)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == refreshToken && !rt.IsRevoked);

        if (token is not null)
        {
            token.IsRevoked = true;
            await _context.SaveChangesAsync();
        }
    }

    // ─── ValidateToken ───────────────────────────────────────────────────────

    public Task<string> ValidateTokenAsync(string token)
    {
        var secret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT secret not configured.");

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secret);

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userId = jwtToken.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
            return Task.FromResult(userId);
        }
        catch
        {
            throw new SecurityTokenException("Invalid or expired token.");
        }
    }

    // ─── RefreshToken ─────────────────────────────────────────────────────────

    public async Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken && !rt.IsRevoked);

        if (storedToken is null || storedToken.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        if (storedToken.User is null || !storedToken.User.IsActive)
            throw new UnauthorizedAccessException("User not found or deactivated.");

        storedToken.IsRevoked = true;

        var (accessToken, expiresAt) = GenerateJwtToken(storedToken.User);
        var newRefreshToken = await CreateRefreshTokenAsync(storedToken.User.UserId);

        await _context.SaveChangesAsync();

        return BuildLoginResponse(storedToken.User, accessToken, expiresAt, newRefreshToken);
    }

    // ─── GetUserById ──────────────────────────────────────────────────────────

    public async Task<UserProfileDto> GetUserByIdAsync(int userId)
    {
        var user = await _userRepository.FindByUserIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");
        return MapToProfileDto(user);
    }

    // ─── UpdateProfile ────────────────────────────────────────────────────────

    public async Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileRequestDto dto)
    {
        var user = await _userRepository.FindByUserIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (dto.FullName is not null) user.FullName = dto.FullName;
        if (dto.Phone is not null) user.Phone = dto.Phone;
        if (dto.PassportNumber is not null) user.PassportNumber = dto.PassportNumber;
        if (dto.Nationality is not null) user.Nationality = dto.Nationality;

        var updated = await _userRepository.UpdateAsync(user);
        return MapToProfileDto(updated);
    }

    // ─── ChangePassword ───────────────────────────────────────────────────────

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequestDto dto)
    {
        var user = await _userRepository.FindByUserIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (user.PasswordHash is null || !BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _userRepository.UpdateAsync(user);
    }

    // ─── DeactivateAccount ────────────────────────────────────────────────────

    public async Task DeactivateAccountAsync(int userId)
    {
        var user = await _userRepository.FindByUserIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        user.IsActive = false;
        await _userRepository.UpdateAsync(user);

        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();
        tokens.ForEach(t => t.IsRevoked = true);
        await _context.SaveChangesAsync();
    }

    // ─── GetAllUsers ──────────────────────────────────────────────────────────

    public async Task<IEnumerable<UserProfileDto>> GetAllUsersAsync()
    {
        var users = await _context.Users.ToListAsync();
        return users.Select(MapToProfileDto);
    }

    public async Task<IEnumerable<UserProfileDto>> GetAllUsersByRoleAsync(UserRole role)
    {
        var users = await _userRepository.FindAllByRoleAsync(role);
        return users.Select(MapToProfileDto);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private (string token, DateTime expiresAt) GenerateJwtToken(User user)
    {
        var secret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT secret not configured.");
        var issuer = _configuration["Jwt:Issuer"] ?? "SkyBooker";
        var audience = _configuration["Jwt:Audience"] ?? "SkyBooker";
        var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private async Task<string> CreateRefreshTokenAsync(int userId)
    {
        var tokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = tokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
        return tokenValue;
    }

    private static LoginResponseDto BuildLoginResponse(User user, string accessToken, DateTime expiresAt, string refreshToken)
        => new()
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString()
        };

    private static UserProfileDto MapToProfileDto(User user) => new()
    {
        UserId = user.UserId,
        FullName = user.FullName,
        Email = user.Email,
        Phone = user.Phone,
        Role = user.Role.ToString(),
        Provider = user.Provider,
        IsActive = user.IsActive,
        PassportNumber = user.PassportNumber,
        Nationality = user.Nationality,
        CreatedAt = user.CreatedAt
    };

    public async Task<UserProfileDto> AssignRoleAsync(int userId, string role)
    {
        var user = await _userRepository.FindByUserIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (!Enum.TryParse<UserRole>(role, true, out var parsedRole))
            throw new InvalidOperationException("Invalid role.");

        // Optional security check
        if (parsedRole == UserRole.Admin)
            throw new UnauthorizedAccessException("Admin role cannot be assigned directly.");

        user.Role = parsedRole;

        var updatedUser = await _userRepository.UpdateAsync(user);

        return MapToProfileDto(updatedUser); // ✅ return DTO
    }
}
