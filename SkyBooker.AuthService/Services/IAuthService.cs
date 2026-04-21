using SkyBooker.AuthService.DTOs;
using SkyBooker.AuthService.Enums;

namespace SkyBooker.AuthService.Services;

public interface IAuthService
{
    Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto dto);
    Task<LoginResponseDto> LoginAsync(LoginRequestDto dto);
    Task LogoutAsync(int userId, string refreshToken);
    Task<string> ValidateTokenAsync(string token);
    Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto);
    Task<UserProfileDto> GetUserByIdAsync(int userId);
    Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileRequestDto dto);
    Task ChangePasswordAsync(int userId, ChangePasswordRequestDto dto);
    Task DeactivateAccountAsync(int userId);
    Task<IEnumerable<UserProfileDto>> GetAllUsersAsync();
    Task<IEnumerable<UserProfileDto>> GetAllUsersByRoleAsync(UserRole role);
    Task<UserProfileDto> AssignRoleAsync(int targetUserId, string role);
}
