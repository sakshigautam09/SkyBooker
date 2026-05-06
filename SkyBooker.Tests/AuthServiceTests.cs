using Moq;
using NUnit.Framework;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using SkyBooker.AuthService.Services;
using SkyBooker.AuthService.Services.Redis;
using SkyBooker.AuthService.Repositories;
using SkyBooker.AuthService.Entities;
using SkyBooker.AuthService.Enums;
using SkyBooker.AuthService.DTOs;
using SkyBooker.AuthService.Data;
using SkyBooker.AuthService.Validators;
using AuthServiceImpl = SkyBooker.AuthService.Services.AuthService;

namespace SkyBooker.Tests;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IUserRepository> _userRepoMock = null!;
    private Mock<IRedisTokenService> _redisMock = null!;
    private IConfiguration _config = null!;
    private AuthDbContext _dbContext = null!;
    private AuthServiceImpl _authService = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _redisMock = new Mock<IRedisTokenService>();

        // In-memory DB for EF operations
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AuthDbContext(options);

        var configData = new Dictionary<string, string?>
        {
            ["Jwt:Secret"]         = "SuperSecretKeyThatIsAtLeast32CharsLong!",
            ["Jwt:Issuer"]         = "SkyBooker",
            ["Jwt:Audience"]       = "SkyBooker",
            ["Jwt:ExpiryMinutes"]  = "60",
            ["AdminRegistration:SecretKey"] = "ADMIN_SECRET_2024"
        };
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _authService = new AuthServiceImpl(
            _userRepoMock.Object,
            _config,
            _dbContext,
            _redisMock.Object);
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    // ─── RegisterAsync ───────────────────────────────────────────────────────

    [Test]
    public async Task RegisterAsync_ValidPassenger_ReturnsRegisterResponse()
    {
        // Arrange
        _userRepoMock.Setup(r => r.ExistsByEmailAsync("john@example.com")).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => { u.UserId = 1; return u; });

        var dto = new RegisterRequestDto
        {
            FullName = "John Doe",
            Email    = "john@example.com",
            Password = "Secret@123",
            Role     = "Passenger"
        };

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        Assert.That(result.Email, Is.EqualTo("john@example.com"));
        Assert.That(result.Role,  Is.EqualTo("Passenger"));
        Assert.That(result.UserId, Is.EqualTo(1));
    }

    [Test]
    public void RegisterAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        _userRepoMock.Setup(r => r.ExistsByEmailAsync("taken@example.com")).ReturnsAsync(true);

        var dto = new RegisterRequestDto
        {
            FullName = "Jane Doe",
            Email    = "taken@example.com",
            Password = "Secret@123"
        };

        Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterAsync(dto));
    }

    [Test]
    public void RegisterAsync_AdminRoleWithoutSecretKey_ThrowsUnauthorizedAccessException()
    {
        _userRepoMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>())).ReturnsAsync(false);

        var dto = new RegisterRequestDto
        {
            FullName = "Evil Admin",
            Email    = "admin@example.com",
            Password = "Secret@123",
            Role     = "Admin",
            AdminSecretKey = "WRONG_SECRET"
        };

        Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.RegisterAsync(dto));
    }

    [Test]
    public async Task RegisterAsync_AdminRoleWithCorrectSecretKey_Succeeds()
    {
        _userRepoMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>())).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => { u.UserId = 2; return u; });

        var dto = new RegisterRequestDto
        {
            FullName = "Real Admin",
            Email    = "realadmin@example.com",
            Password = "Secret@123",
            Role     = "Admin",
            AdminSecretKey = "ADMIN_SECRET_2024"
        };

        var result = await _authService.RegisterAsync(dto);
        Assert.That(result.Role, Is.EqualTo("Admin"));
    }

    [Test]
    public void RegisterAsync_InvalidRole_ThrowsInvalidOperationException()
    {
        _userRepoMock.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>())).ReturnsAsync(false);

        var dto = new RegisterRequestDto
        {
            FullName = "Unknown",
            Email    = "unknown@example.com",
            Password = "Secret@123",
            Role     = "SuperUser"
        };

        Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterAsync(dto));
    }

    // ─── LoginAsync ───────────────────────────────────────────────────────────

    [Test]
    public async Task LoginAsync_ValidCredentials_ReturnsLoginResponse()
    {
        var hashedPwd = BCrypt.Net.BCrypt.HashPassword("Secret@123");
        var user = new User
        {
            UserId       = 10,
            FullName     = "John Doe",
            Email        = "john@example.com",
            PasswordHash = hashedPwd,
            Role         = UserRole.Passenger,
            IsActive     = true
        };

        _userRepoMock.Setup(r => r.FindByEmailAsync("john@example.com")).ReturnsAsync(user);
        _redisMock.Setup(r => r.StoreRefreshTokenAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        var dto = new LoginRequestDto { Email = "john@example.com", Password = "Secret@123" };

        var result = await _authService.LoginAsync(dto);

        Assert.That(result.AccessToken, Is.Not.Empty);
        Assert.That(result.UserId, Is.EqualTo(10));
        Assert.That(result.Role, Is.EqualTo("Passenger"));
    }

    [Test]
    public void LoginAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        var user = new User
        {
            UserId       = 10,
            Email        = "john@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword@1"),
            IsActive     = true
        };
        _userRepoMock.Setup(r => r.FindByEmailAsync("john@example.com")).ReturnsAsync(user);

        var dto = new LoginRequestDto { Email = "john@example.com", Password = "WrongPassword@1" };
        Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(dto));
    }

    [Test]
    public void LoginAsync_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        _userRepoMock.Setup(r => r.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        var dto = new LoginRequestDto { Email = "ghost@example.com", Password = "Secret@123" };
        Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(dto));
    }

    [Test]
    public void LoginAsync_DeactivatedAccount_ThrowsUnauthorizedAccessException()
    {
        var user = new User
        {
            UserId       = 5,
            Email        = "inactive@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Secret@123"),
            IsActive     = false
        };
        _userRepoMock.Setup(r => r.FindByEmailAsync("inactive@example.com")).ReturnsAsync(user);
        var dto = new LoginRequestDto { Email = "inactive@example.com", Password = "Secret@123" };
        Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(dto));
    }

    // ─── GetUserByIdAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task GetUserByIdAsync_ExistingUser_ReturnsUserProfile()
    {
        var user = new User { UserId = 1, FullName = "Alice", Email = "alice@example.com", Role = UserRole.Passenger, IsActive = true, Provider = "local" };
        _userRepoMock.Setup(r => r.FindByUserIdAsync(1)).ReturnsAsync(user);

        var result = await _authService.GetUserByIdAsync(1);

        Assert.That(result.Email, Is.EqualTo("alice@example.com"));
        Assert.That(result.FullName, Is.EqualTo("Alice"));
    }

    [Test]
    public void GetUserByIdAsync_NonExistingUser_ThrowsKeyNotFoundException()
    {
        _userRepoMock.Setup(r => r.FindByUserIdAsync(999)).ReturnsAsync((User?)null);
        Assert.ThrowsAsync<KeyNotFoundException>(() => _authService.GetUserByIdAsync(999));
    }

    // ─── UpdateProfileAsync ───────────────────────────────────────────────────

    [Test]
    public async Task UpdateProfileAsync_ValidUpdate_ReturnsUpdatedProfile()
    {
        var user = new User { UserId = 1, FullName = "Old Name", Email = "user@example.com", Role = UserRole.Passenger, IsActive = true, Provider = "local" };
        _userRepoMock.Setup(r => r.FindByUserIdAsync(1)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var dto = new UpdateProfileRequestDto { FullName = "New Name", Phone = "+919876543210" };
        var result = await _authService.UpdateProfileAsync(1, dto);

        Assert.That(result.FullName, Is.EqualTo("New Name"));
    }

    // ─── ChangePasswordAsync ──────────────────────────────────────────────────

    [Test]
    public void ChangePasswordAsync_WrongCurrentPassword_ThrowsUnauthorizedAccessException()
    {
        var user = new User
        {
            UserId       = 1,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass@123")
        };
        _userRepoMock.Setup(r => r.FindByUserIdAsync(1)).ReturnsAsync(user);

        var dto = new ChangePasswordRequestDto { CurrentPassword = "WrongPass@123", NewPassword = "NewPass@123" };
        Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.ChangePasswordAsync(1, dto));
    }

    // ─── AssignRoleAsync ──────────────────────────────────────────────────────

    [Test]
    public void AssignRoleAsync_AdminRole_ThrowsUnauthorizedAccessException()
    {
        var user = new User { UserId = 1, Role = UserRole.Passenger, FullName = "X", Email = "x@x.com", IsActive = true, Provider = "local" };
        _userRepoMock.Setup(r => r.FindByUserIdAsync(1)).ReturnsAsync(user);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.AssignRoleAsync(1, "Admin"));
    }

    [Test]
    public async Task AssignRoleAsync_ValidRole_UpdatesRole()
    {
        var user = new User { UserId = 1, Role = UserRole.Passenger, FullName = "X", Email = "x@x.com", IsActive = true, Provider = "local" };
        _userRepoMock.Setup(r => r.FindByUserIdAsync(1)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _authService.AssignRoleAsync(1, "AirlineStaff");
        Assert.That(result.Role, Is.EqualTo("AirlineStaff"));
    }
}

// ─── Validator Tests ──────────────────────────────────────────────────────────

[TestFixture]
public class RegisterRequestValidatorTests
{
    private RegisterRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new RegisterRequestValidator();

    [Test]
    public void Validate_ValidPassengerDto_PassesValidation()
    {
        var dto = new RegisterRequestDto
        {
            FullName = "John Doe",
            Email    = "john@example.com",
            Password = "Secret@123",
            Role     = "Passenger"
        };
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_EmptyFullName_FailsValidation()
    {
        var dto = new RegisterRequestDto { FullName = "", Email = "john@example.com", Password = "Secret@123" };
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "FullName"), Is.True);
    }

    [Test]
    public void Validate_InvalidEmail_FailsValidation()
    {
        var dto = new RegisterRequestDto { FullName = "John", Email = "not-an-email", Password = "Secret@123" };
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "Email"), Is.True);
    }

    [Test]
    public void Validate_WeakPassword_NoUpperCase_FailsValidation()
    {
        var dto = new RegisterRequestDto { FullName = "John", Email = "john@example.com", Password = "secret@123" };
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_WeakPassword_NoSpecialChar_FailsValidation()
    {
        var dto = new RegisterRequestDto { FullName = "John", Email = "john@example.com", Password = "Secret1234" };
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_AdminRoleWithoutSecretKey_FailsValidation()
    {
        var dto = new RegisterRequestDto
        {
            FullName = "Admin User",
            Email    = "admin@example.com",
            Password = "Secret@123",
            Role     = "Admin"
            // AdminSecretKey intentionally missing
        };
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "AdminSecretKey"), Is.True);
    }

    [Test]
    public void Validate_InvalidRole_FailsValidation()
    {
        var dto = new RegisterRequestDto { FullName = "John", Email = "john@example.com", Password = "Secret@123", Role = "SuperUser" };
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void Validate_InvalidPhoneFormat_FailsValidation()
    {
        var dto = new RegisterRequestDto { FullName = "John", Email = "john@example.com", Password = "Secret@123", Phone = "abc123" };
        var result = _validator.Validate(dto);
        Assert.That(result.IsValid, Is.False);
    }
}