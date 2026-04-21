using Microsoft.EntityFrameworkCore;
using SkyBooker.AuthService.Data;
using SkyBooker.AuthService.Entities;
using SkyBooker.AuthService.Enums;

namespace SkyBooker.AuthService.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _context;

    public UserRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<User?> FindByEmailAsync(string email)
        => await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> FindByUserIdAsync(int userId)
        => await _context.Users.FindAsync(userId);

    public async Task<bool> ExistsByEmailAsync(string email)
        => await _context.Users.AnyAsync(u => u.Email == email);

    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<IEnumerable<User>> FindAllByRoleAsync(UserRole role)
        => await _context.Users.Where(u => u.Role == role).ToListAsync();

    public async Task<User?> FindByPhoneAsync(string phone)
        => await _context.Users.FirstOrDefaultAsync(u => u.Phone == phone);

    public async Task<User?> FindByPassportNumberAsync(string passportNumber)
        => await _context.Users.FirstOrDefaultAsync(u => u.PassportNumber == passportNumber);

    public async Task<bool> DeleteByUserIdAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null) return false;
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<User> UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }
}
