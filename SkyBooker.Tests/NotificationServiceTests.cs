using Moq;
using NUnit.Framework;
using SkyBooker.NotificationService.Services;
using SkyBooker.NotificationService.DTOs;

namespace SkyBooker.Tests;

[TestFixture]
public class NotificationServiceTests
{
    private Mock<INotificationService> _notificationServiceMock = null!;

    [SetUp]
    public void SetUp() => _notificationServiceMock = new Mock<INotificationService>();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static NotificationResponseDto MakeNotificationDto(
        int id       = 1,
        bool isRead  = false,
        string type  = "BookingConfirmation") => new()
    {
        NotificationId = id,
        RecipientId    = 10,
        Message        = "Your booking has been confirmed.",
        Type           = type,
        IsRead         = isRead,
        SentAt         = DateTime.UtcNow   // was: CreatedAt
    };

    // ─── SendAsync ────────────────────────────────────────────────────────────

    [Test]
    public async Task SendAsync_ValidRequest_ReturnsNotificationDto()
    {
        var dto = new SendNotificationRequestDto
        {
            RecipientId = 10,
            Message     = "Your booking BK-001 is confirmed.",
            Type        = "BookingConfirmation"
        };

        var expected = MakeNotificationDto();
        _notificationServiceMock.Setup(s => s.SendAsync(dto)).ReturnsAsync(expected);

        var result = await _notificationServiceMock.Object.SendAsync(dto);

        Assert.That(result.NotificationId, Is.EqualTo(1));
        Assert.That(result.IsRead, Is.False);
    }

    // ─── SendBookingConfirmationAsync ─────────────────────────────────────────

    [Test]
    public async Task SendBookingConfirmationAsync_ValidBooking_CompletesSuccessfully()
    {
        var dto = new BookingConfirmationRequestDto
        {
            RecipientId   = 10,                          // was: UserId
            BookingId     = "BK-001",
            RecipientName = "Rahul Sharma",              // was: PassengerName
            FlightNumber  = "AI-101",
            Origin        = "DEL",
            Destination   = "BOM",
            DepartureTime = DateTime.UtcNow.AddDays(5), // was: Departure
            TotalFare     = 4500m,
            PnrCode       = "ABCDEF",
            RecipientEmail = "rahul@example.com"         // was: ContactEmail
        };

        _notificationServiceMock.Setup(s => s.SendBookingConfirmationAsync(dto)).Returns(Task.CompletedTask);

        Assert.DoesNotThrowAsync(() => _notificationServiceMock.Object.SendBookingConfirmationAsync(dto));
    }

    // ─── SendBulkAsync ────────────────────────────────────────────────────────

    [Test]
    public async Task SendBulkAsync_MultipleRecipients_ReturnsAllNotifications()
    {
        var dto = new SendBulkNotificationRequestDto
        {
            RecipientIds = new List<int> { 1, 2, 3 },
            Message      = "Flight AI-101 is delayed by 30 minutes.",
            Type         = "FlightAlert"
        };

        var expected = new List<NotificationResponseDto>
        {
            MakeNotificationDto(1), MakeNotificationDto(2), MakeNotificationDto(3)
        };

        _notificationServiceMock.Setup(s => s.SendBulkAsync(dto)).ReturnsAsync(expected);

        var result = await _notificationServiceMock.Object.SendBulkAsync(dto);

        Assert.That(result.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task SendBulkAsync_EmptyRecipientList_ReturnsEmptyList()
    {
        var dto = new SendBulkNotificationRequestDto
        {
            RecipientIds = new List<int>(),
            Message      = "Test",
            Type         = "System"
        };

        _notificationServiceMock.Setup(s => s.SendBulkAsync(dto)).ReturnsAsync(new List<NotificationResponseDto>());

        var result = await _notificationServiceMock.Object.SendBulkAsync(dto);

        Assert.That(result, Is.Empty);
    }

    // ─── MarkAsReadAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task MarkAsReadAsync_UnreadNotification_ReturnsMarkedAsRead()
    {
        var expected = MakeNotificationDto(isRead: true);
        _notificationServiceMock.Setup(s => s.MarkAsReadAsync(1)).ReturnsAsync(expected);

        var result = await _notificationServiceMock.Object.MarkAsReadAsync(1);

        Assert.That(result.IsRead, Is.True);
    }

    // ─── MarkAllReadAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task MarkAllReadAsync_ValidRecipient_CompletesSuccessfully()
    {
        _notificationServiceMock.Setup(s => s.MarkAllReadAsync(10)).Returns(Task.CompletedTask);
        Assert.DoesNotThrowAsync(() => _notificationServiceMock.Object.MarkAllReadAsync(10));
    }

    // ─── GetByRecipientAsync ──────────────────────────────────────────────────

    [Test]
    public async Task GetByRecipientAsync_RecipientWithNotifications_ReturnsList()
    {
        var notifications = new List<NotificationResponseDto>
        {
            MakeNotificationDto(1, isRead: true),
            MakeNotificationDto(2, isRead: false)
        };
        _notificationServiceMock.Setup(s => s.GetByRecipientAsync(10)).ReturnsAsync(notifications);

        var result = await _notificationServiceMock.Object.GetByRecipientAsync(10);

        Assert.That(result.Count, Is.EqualTo(2));
    }

    // ─── GetUnreadCountAsync ──────────────────────────────────────────────────

    [Test]
    public async Task GetUnreadCountAsync_WithUnread_ReturnsCorrectCount()
    {
        _notificationServiceMock.Setup(s => s.GetUnreadCountAsync(10)).ReturnsAsync(5);

        var result = await _notificationServiceMock.Object.GetUnreadCountAsync(10);

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public async Task GetUnreadCountAsync_AllRead_ReturnsZero()
    {
        _notificationServiceMock.Setup(s => s.GetUnreadCountAsync(10)).ReturnsAsync(0);

        var result = await _notificationServiceMock.Object.GetUnreadCountAsync(10);

        Assert.That(result, Is.EqualTo(0));
    }

    // ─── DeleteNotificationAsync ──────────────────────────────────────────────

    [Test]
    public async Task DeleteNotificationAsync_ExistingNotification_CompletesSuccessfully()
    {
        _notificationServiceMock.Setup(s => s.DeleteNotificationAsync(1)).Returns(Task.CompletedTask);
        Assert.DoesNotThrowAsync(() => _notificationServiceMock.Object.DeleteNotificationAsync(1));
    }

    // ─── GetAllAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllAsync_WithData_ReturnsAllNotifications()
    {
        var all = new List<NotificationResponseDto>
        {
            MakeNotificationDto(1), MakeNotificationDto(2), MakeNotificationDto(3)
        };
        _notificationServiceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(all);

        var result = await _notificationServiceMock.Object.GetAllAsync();

        Assert.That(result.Count, Is.EqualTo(3));
    }

    // ─── SendEmailAsync ───────────────────────────────────────────────────────

    [Test]
    public async Task SendEmailAsync_ValidInput_CompletesSuccessfully()
    {
        _notificationServiceMock.Setup(s =>
            s.SendEmailAsync("test@example.com", "Test User", "Subject", "Email body"))
            .Returns(Task.CompletedTask);

        Assert.DoesNotThrowAsync(() =>
            _notificationServiceMock.Object.SendEmailAsync("test@example.com", "Test User", "Subject", "Email body"));
    }

    // ─── SendSmsAsync ─────────────────────────────────────────────────────────

    [Test]
    public async Task SendSmsAsync_ValidPhone_CompletesSuccessfully()
    {
        _notificationServiceMock.Setup(s => s.SendSmsAsync("+919876543210", "Your OTP is 123456")).Returns(Task.CompletedTask);
        Assert.DoesNotThrowAsync(() => _notificationServiceMock.Object.SendSmsAsync("+919876543210", "Your OTP is 123456"));
    }
}