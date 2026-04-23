using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkyBooker.NotificationService.DTOs;
using SkyBooker.NotificationService.Entities;
using SkyBooker.NotificationService.Enums;
using SkyBooker.NotificationService.Repositories;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace SkyBooker.NotificationService.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        IConfiguration configuration,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _configuration = configuration;
        _logger = logger;

        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<NotificationResponseDto> SendAsync(SendNotificationRequestDto dto)
    {
        if (!Enum.TryParse<NotificationType>(dto.Type, true, out var type))
            type = NotificationType.General;

        if (!Enum.TryParse<NotificationChannel>(dto.Channel, true, out var channel))
            channel = NotificationChannel.App;

        var notification = new Notification
        {
            RecipientId = dto.RecipientId,
            Type = type,
            Title = dto.Title,
            Message = dto.Message,
            Channel = channel,
            RelatedBookingId = dto.RelatedBookingId,
            RecipientEmail = dto.RecipientEmail,
            RecipientPhone = dto.RecipientPhone,
            IsRead = false,
            SentAt = DateTime.UtcNow
        };

        var created = await _notificationRepository.CreateAsync(notification);

        // Dispatch to the appropriate channel
        try
        {
            if (channel == NotificationChannel.Email && !string.IsNullOrEmpty(dto.RecipientEmail))
                await SendEmailAsync(dto.RecipientEmail, "", dto.Title, dto.Message);

            if (channel == NotificationChannel.Sms && !string.IsNullOrEmpty(dto.RecipientPhone))
                await SendSmsAsync(dto.RecipientPhone, dto.Message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Channel dispatch failed for NotificationId={Id}, Channel={Channel}",
                created.NotificationId, channel);
            // Don't fail the whole operation if external channel fails
            // Notification is still saved in DB for in-app delivery
        }

        _logger.LogInformation(
            "Notification sent: RecipientId={RecipientId}, Type={Type}, Channel={Channel}",
            dto.RecipientId, type, channel);

        return MapToDto(created);
    }

    public async Task SendBookingConfirmationAsync(BookingConfirmationRequestDto dto)
    {
        // 1 — Save in-app notification
        var notification = new Notification
        {
            RecipientId = dto.RecipientId,
            Type = NotificationType.BookingConfirmed,
            Title = "Booking Confirmed!",
            Message = $"Your booking {dto.PnrCode} for flight {dto.FlightNumber} " +
                      $"({dto.Origin} → {dto.Destination}) on " +
                      $"{dto.DepartureTime:dd MMM yyyy HH:mm} is confirmed. " +
                      $"Total: ₹{dto.TotalFare:F2}",
            Channel = NotificationChannel.App,
            RelatedBookingId = dto.BookingId,
            IsRead = false,
            SentAt = DateTime.UtcNow
        };

        await _notificationRepository.CreateAsync(notification);

        // 2 — Send email with e-ticket PDF attachment via MailKit
        if (!string.IsNullOrEmpty(dto.RecipientEmail))
        {
            try
            {
                await SendBookingConfirmationEmailAsync(dto);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Email dispatch failed for BookingId={BookingId}", dto.BookingId);
            }
        }

        // 3 — Send SMS via Twilio
        if (!string.IsNullOrEmpty(dto.RecipientPhone))
        {
            try
            {
                var smsMessage = $"SkyBooker: Booking confirmed! PNR: {dto.PnrCode}, " +
                                 $"Flight: {dto.FlightNumber}, " +
                                 $"{dto.Origin}→{dto.Destination}, " +
                                 $"{dto.DepartureTime:dd MMM HH:mm}. " +
                                 $"Amount: ₹{dto.TotalFare:F2}";
                await SendSmsAsync(dto.RecipientPhone, smsMessage);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "SMS dispatch failed for BookingId={BookingId}", dto.BookingId);
            }
        }

        _logger.LogInformation(
            "Booking confirmation sent: PNR={Pnr}, RecipientId={RecipientId}",
            dto.PnrCode, dto.RecipientId);
    }

    public async Task<IList<NotificationResponseDto>> SendBulkAsync(
        SendBulkNotificationRequestDto dto)
    {
        if (!Enum.TryParse<NotificationType>(dto.Type, true, out var type))
            type = NotificationType.General;

        if (!Enum.TryParse<NotificationChannel>(dto.Channel, true, out var channel))
            channel = NotificationChannel.App;

        var notifications = dto.RecipientIds.Select(recipientId => new Notification
        {
            RecipientId = recipientId,
            Type = type,
            Title = dto.Title,
            Message = dto.Message,
            Channel = channel,
            RelatedBookingId = dto.RelatedBookingId,
            IsRead = false,
            SentAt = DateTime.UtcNow
        }).ToList();

        var results = new List<NotificationResponseDto>();
        foreach (var notification in notifications)
        {
            var created = await _notificationRepository.CreateAsync(notification);
            results.Add(MapToDto(created));
        }

        _logger.LogInformation(
            "Bulk notification sent to {Count} recipients. Type={Type}",
            dto.RecipientIds.Count, type);

        return results;
    }

    public async Task<NotificationResponseDto> MarkAsReadAsync(int notificationId)
    {
        var notification = await _notificationRepository.FindByNotificationIdAsync(notificationId)
            ?? throw new KeyNotFoundException($"Notification {notificationId} not found.");

        notification.IsRead = true;
        var updated = await _notificationRepository.UpdateAsync(notification);
        return MapToDto(updated);
    }

    public async Task MarkAllReadAsync(int recipientId)
    {
        var unread = await _notificationRepository
            .FindByRecipientIdAndIsReadAsync(recipientId, false);

        foreach (var n in unread)
            n.IsRead = true;

        await _notificationRepository.UpdateRangeAsync(unread);

        _logger.LogInformation(
            "Marked {Count} notifications as read for RecipientId={RecipientId}",
            unread.Count, recipientId);
    }

    public async Task<IList<NotificationResponseDto>> GetByRecipientAsync(int recipientId)
    {
        var notifications = await _notificationRepository.FindByRecipientIdAsync(recipientId);
        return notifications.Select(MapToDto).ToList();
    }

    public async Task<int> GetUnreadCountAsync(int recipientId)
        => await _notificationRepository.CountByRecipientIdAndIsReadAsync(recipientId, false);

    public async Task DeleteNotificationAsync(int notificationId)
    {
        var notification = await _notificationRepository
            .FindByNotificationIdAsync(notificationId)
            ?? throw new KeyNotFoundException($"Notification {notificationId} not found.");

        await _notificationRepository.DeleteByNotificationIdAsync(notificationId);
    }

    public async Task<IList<NotificationResponseDto>> GetAllAsync()
    {
        var notifications = await _notificationRepository.FindAllAsync();
        return notifications.Select(MapToDto).ToList();
    }

    public async Task SendEmailAsync(
        string toEmail, string toName, string subject, string body)
    {
        var smtpHost = _configuration["Email:SmtpHost"] ?? string.Empty;
        var smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 587);
        var senderEmail = _configuration["Email:SenderEmail"] ?? string.Empty;
        var senderName = _configuration["Email:SenderName"] ?? "SkyBooker";
        var password = _configuration["Email:Password"] ?? string.Empty;

        // Skip if not configured — for evaluation
        if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(password)
            || password == "YOUR_EMAIL_APP_PASSWORD")
        {
            _logger.LogWarning(
                "Email not configured — skipping email dispatch to {Email}", toEmail);
            return;
        }

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(senderName, senderEmail));
        email.To.Add(new MailboxAddress(toName, toEmail));
        email.Subject = subject;
        email.Body = new TextPart(TextFormat.Html) { Text = body };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(senderEmail, password);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);

        _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
    }

    public Task SendSmsAsync(string toPhone, string message)
    {
        var accountSid = _configuration["Twilio:AccountSid"] ?? string.Empty;
        var authToken = _configuration["Twilio:AuthToken"] ?? string.Empty;
        var fromNumber = _configuration["Twilio:FromNumber"] ?? string.Empty;

        // Skip if not configured — for evaluation
        if (string.IsNullOrEmpty(accountSid)
            || accountSid == "YOUR_TWILIO_ACCOUNT_SID")
        {
            _logger.LogWarning(
                "Twilio not configured — skipping SMS dispatch to {Phone}", toPhone);
            return Task.CompletedTask;
        }

        TwilioClient.Init(accountSid, authToken);

        MessageResource.Create(
            body: message,
            from: new Twilio.Types.PhoneNumber(fromNumber),
            to: new Twilio.Types.PhoneNumber(toPhone)
        );

        _logger.LogInformation("SMS sent to {Phone}", toPhone);
        return Task.CompletedTask;
    }

    // ── Private Helpers ────────────────────────────────────────────────────────

    private async Task SendBookingConfirmationEmailAsync(BookingConfirmationRequestDto dto)
    {
        var smtpHost = _configuration["Email:SmtpHost"] ?? string.Empty;
        var smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 587);
        var senderEmail = _configuration["Email:SenderEmail"] ?? string.Empty;
        var senderName = _configuration["Email:SenderName"] ?? "SkyBooker";
        var password = _configuration["Email:Password"] ?? string.Empty;

        if (string.IsNullOrEmpty(smtpHost) || password == "YOUR_EMAIL_APP_PASSWORD")
        {
            _logger.LogWarning(
                "Email not configured — skipping confirmation email for PNR={Pnr}", dto.PnrCode);
            return;
        }

        // Generate e-ticket PDF using QuestPDF
        var pdfBytes = GenerateETicketPdf(dto);

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(senderName, senderEmail));
        email.To.Add(new MailboxAddress(dto.RecipientName, dto.RecipientEmail));
        email.Subject = $"SkyBooker — Booking Confirmed! PNR: {dto.PnrCode}";

        var builder = new BodyBuilder
        {
            HtmlBody = BuildBookingConfirmationHtml(dto)
        };

        // Attach e-ticket PDF using MimeKit MimePart API
        builder.Attachments.Add(
            $"eticket_{dto.PnrCode}.pdf",
            pdfBytes,
            new ContentType("application", "pdf"));

        email.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(senderEmail, password);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);

        _logger.LogInformation(
            "Booking confirmation email with e-ticket sent to {Email}, PNR={Pnr}",
            dto.RecipientEmail, dto.PnrCode);
    }

    private static byte[] GenerateETicketPdf(BookingConfirmationRequestDto dto)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("SkyBooker")
                                .FontSize(26).Bold().FontColor(Colors.Blue.Darken2);
                            left.Item().Text("E-Ticket / Boarding Pass")
                                .FontSize(13).FontColor(Colors.Grey.Darken1);
                        });
                        row.AutoItem().Column(right =>
                        {
                            right.Item().AlignRight().Text("PNR")
                                .FontSize(11).FontColor(Colors.Grey.Medium);
                            right.Item().AlignRight().Text(dto.PnrCode)
                                .FontSize(22).Bold().FontColor(Colors.Blue.Darken2);
                        });
                    });
                    col.Item().PaddingTop(8).LineHorizontal(2).LineColor(Colors.Blue.Darken2);
                });

                page.Content().PaddingTop(20).Column(col =>
                {
                    // Passenger info
                    col.Item().Background(Colors.Blue.Lighten5).Padding(12).Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("Passenger").FontSize(10)
                                .FontColor(Colors.Grey.Medium);
                            left.Item().Text(dto.RecipientName).Bold().FontSize(14);
                        });
                        row.RelativeItem().Column(right =>
                        {
                            right.Item().Text("Seat").FontSize(10)
                                .FontColor(Colors.Grey.Medium);
                            right.Item().Text(dto.SeatNumber).Bold().FontSize(14);
                        });
                    });

                    col.Item().PaddingTop(16).Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("FROM").FontSize(10)
                                .FontColor(Colors.Grey.Medium);
                            left.Item().Text(dto.Origin).FontSize(24).Bold()
                                .FontColor(Colors.Blue.Darken3);
                        });
                        row.AutoItem().PaddingHorizontal(20).AlignMiddle()
                            .Text("→").FontSize(20).FontColor(Colors.Grey.Medium);
                        row.RelativeItem().Column(right =>
                        {
                            right.Item().Text("TO").FontSize(10)
                                .FontColor(Colors.Grey.Medium);
                            right.Item().Text(dto.Destination).FontSize(24).Bold()
                                .FontColor(Colors.Blue.Darken3);
                        });
                    });

                    col.Item().PaddingTop(16).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn();
                            cols.RelativeColumn();
                            cols.RelativeColumn();
                        });

                        table.Cell().Padding(8).Column(c =>
                        {
                            c.Item().Text("Flight").FontSize(10)
                                .FontColor(Colors.Grey.Medium);
                            c.Item().Text(dto.FlightNumber).Bold();
                        });
                        table.Cell().Padding(8).Column(c =>
                        {
                            c.Item().Text("Departure").FontSize(10)
                                .FontColor(Colors.Grey.Medium);
                            c.Item().Text(dto.DepartureTime.ToString("dd MMM yyyy")).Bold();
                            c.Item().Text(dto.DepartureTime.ToString("HH:mm")).Bold()
                                .FontSize(16).FontColor(Colors.Blue.Darken2);
                        });
                        table.Cell().Padding(8).Column(c =>
                        {
                            c.Item().Text("Total Fare").FontSize(10)
                                .FontColor(Colors.Grey.Medium);
                            c.Item().Text($"₹{dto.TotalFare:F2}").Bold()
                                .FontColor(Colors.Green.Darken3);
                        });
                    });

                    col.Item().PaddingTop(20).LineHorizontal(1)
                        .LineColor(Colors.Grey.Lighten2);

                    col.Item().PaddingTop(10).Background(Colors.Grey.Lighten4)
                        .Padding(10).Text(
                            "Please arrive at the airport at least 2 hours before departure. " +
                            "This e-ticket is valid as your travel document. " +
                            "Web check-in opens 24 hours before departure.")
                        .FontSize(9).FontColor(Colors.Grey.Darken1).Italic();
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("SkyBooker Platform • ").FontSize(9)
                        .FontColor(Colors.Grey.Medium);
                    text.Span("Search. Book. Fly. Effortlessly.")
                        .FontSize(9).FontColor(Colors.Grey.Medium).Italic();
                });
            });
        }).GeneratePdf();
    }

    private static string BuildBookingConfirmationHtml(BookingConfirmationRequestDto dto)
    {
        return $@"
<html><body style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>
  <div style='background:#1a56db;padding:20px;text-align:center;'>
    <h1 style='color:white;margin:0;'>SkyBooker</h1>
    <p style='color:#b3d0ff;margin:5px 0 0;'>Booking Confirmed!</p>
  </div>
  <div style='padding:24px;background:#f9fafb;'>
    <h2 style='color:#1a56db;'>Hi {dto.RecipientName},</h2>
    <p>Your booking is confirmed. Here are your travel details:</p>
    <div style='background:white;border-radius:8px;padding:20px;margin:16px 0;
                border-left:4px solid #1a56db;'>
      <table width='100%'>
        <tr><td><b>PNR Code</b></td><td style='color:#1a56db;font-size:20px;font-weight:bold;'>{dto.PnrCode}</td></tr>
        <tr><td><b>Flight</b></td><td>{dto.FlightNumber}</td></tr>
        <tr><td><b>Route</b></td><td>{dto.Origin} → {dto.Destination}</td></tr>
        <tr><td><b>Departure</b></td><td>{dto.DepartureTime:dd MMM yyyy HH:mm}</td></tr>
        <tr><td><b>Seat</b></td><td>{dto.SeatNumber}</td></tr>
        <tr><td><b>Total Fare</b></td><td style='color:#057a55;font-weight:bold;'>₹{dto.TotalFare:F2}</td></tr>
      </table>
    </div>
    <p style='color:#6b7280;font-size:13px;'>
      Your e-ticket is attached to this email as a PDF. 
      Web check-in opens 24 hours before departure.
    </p>
  </div>
  <div style='background:#1a56db;padding:12px;text-align:center;'>
    <p style='color:#b3d0ff;margin:0;font-size:12px;'>
      SkyBooker • Search. Book. Fly. Effortlessly.
    </p>
  </div>
</body></html>";
    }

    private static NotificationResponseDto MapToDto(Notification n) => new()
    {
        NotificationId = n.NotificationId,
        RecipientId = n.RecipientId,
        Type = n.Type.ToString(),
        Title = n.Title,
        Message = n.Message,
        Channel = n.Channel.ToString(),
        RelatedBookingId = n.RelatedBookingId,
        IsRead = n.IsRead,
        SentAt = n.SentAt
    };
}
