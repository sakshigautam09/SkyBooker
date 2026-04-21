using SkyBooker.AuthService.Entities;
using SkyBooker.AuthService.Enums;

namespace SkyBooker.AuthService.Repositories;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email);
    Task<User?> FindByUserIdAsync(int userId);
    Task<bool> ExistsByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    Task<IEnumerable<User>> FindAllByRoleAsync(UserRole role);
    Task<User?> FindByPhoneAsync(string phone);
    Task<User?> FindByPassportNumberAsync(string passportNumber);
    Task<bool> DeleteByUserIdAsync(int userId);
    Task<User> UpdateAsync(User user);
}
