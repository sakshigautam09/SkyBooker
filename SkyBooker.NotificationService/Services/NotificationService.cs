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
using SendGrid;
using SendGrid.Helpers.Mail;

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
        _configuration          = configuration;
        _logger                 = logger;

        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ── Send Single Notification ──────────────────────────────────────────────

    public async Task<NotificationResponseDto> SendAsync(SendNotificationRequestDto dto)
    {
        if (!Enum.TryParse<NotificationType>(dto.Type, true, out var type))
            type = NotificationType.General;

        if (!Enum.TryParse<NotificationChannel>(dto.Channel, true, out var channel))
            channel = NotificationChannel.App;

        var notification = new Notification
        {
            RecipientId      = dto.RecipientId,
            Type             = type,
            Title            = dto.Title,
            Message          = dto.Message,
            Channel          = channel,
            RelatedBookingId = dto.RelatedBookingId,
            RecipientEmail   = dto.RecipientEmail,
            RecipientPhone   = dto.RecipientPhone,
            IsRead           = false,
            SentAt           = DateTime.UtcNow
        };

        var created = await _notificationRepository.CreateAsync(notification);

        // Dispatch to external channel — failure does NOT abort the operation
        // The notification is always persisted for in-app delivery
        try
        {
            if (channel == NotificationChannel.Email
                && !string.IsNullOrEmpty(dto.RecipientEmail))
            {
                await SendEmailAsync(dto.RecipientEmail, "", dto.Title,
                    BuildSimpleHtml(dto.Title, dto.Message));
            }

            if (channel == NotificationChannel.Sms
                && !string.IsNullOrEmpty(dto.RecipientPhone))
            {
                await SendSmsAsync(dto.RecipientPhone, $"[SkyBooker] {dto.Title}: {dto.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Channel dispatch failed for NotificationId={Id}, Channel={Channel}",
                created.NotificationId, channel);
        }

        _logger.LogInformation(
            "Notification sent: RecipientId={RecipientId}, Type={Type}, Channel={Channel}",
            dto.RecipientId, type, channel);

        return MapToDto(created);
    }

    // ── Booking Confirmation (App + Email with PDF + SMS) ─────────────────────

    public async Task SendBookingConfirmationAsync(BookingConfirmationRequestDto dto)
    {
        // 1 — Persist in-app notification
        var notification = new Notification
        {
            RecipientId      = dto.RecipientId,
            Type             = NotificationType.BookingConfirmed,
            Title            = "Booking Confirmed! ✈",
            Message          = $"Your booking {dto.PnrCode} for flight {dto.FlightNumber} " +
                               $"({dto.Origin} → {dto.Destination}) on " +
                               $"{dto.DepartureTime:dd MMM yyyy HH:mm} is confirmed. " +
                               $"Total: ₹{dto.TotalFare:F2}",
            Channel          = NotificationChannel.App,
            RelatedBookingId = dto.BookingId,
            IsRead           = false,
            SentAt           = DateTime.UtcNow
        };

        await _notificationRepository.CreateAsync(notification);

        // 2 — Email with e-ticket PDF attachment
        if (!string.IsNullOrEmpty(dto.RecipientEmail))
        {
            try
            {
                await SendBookingConfirmationEmailAsync(dto);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Email failed for BookingId={BookingId}", dto.BookingId);
            }
        }

        // 3 — SMS via Twilio
        if (!string.IsNullOrEmpty(dto.RecipientPhone))
        {
            try
            {
                var sms = $"[SkyBooker] Booking Confirmed! ✈\n" +
                          $"PNR: {dto.PnrCode}\n" +
                          $"Flight: {dto.FlightNumber} | {dto.Origin}→{dto.Destination}\n" +
                          $"Dep: {dto.DepartureTime:dd MMM yyyy HH:mm}\n" +
                          $"Seat: {dto.SeatNumber}\n" +
                          $"Total: ₹{dto.TotalFare:F2}";

                await SendSmsAsync(dto.RecipientPhone, sms);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "SMS failed for BookingId={BookingId}", dto.BookingId);
            }
        }

        _logger.LogInformation(
            "Booking confirmation sent: PNR={Pnr}, RecipientId={RecipientId}",
            dto.PnrCode, dto.RecipientId);
    }

    // ── Bulk Notification ─────────────────────────────────────────────────────

    public async Task<IList<NotificationResponseDto>> SendBulkAsync(
        SendBulkNotificationRequestDto dto)
    {
        if (!Enum.TryParse<NotificationType>(dto.Type, true, out var type))
            type = NotificationType.General;

        if (!Enum.TryParse<NotificationChannel>(dto.Channel, true, out var channel))
            channel = NotificationChannel.App;

        var results = new List<NotificationResponseDto>();

        foreach (var recipientId in dto.RecipientIds)
        {
            var notification = new Notification
            {
                RecipientId      = recipientId,
                Type             = type,
                Title            = dto.Title,
                Message          = dto.Message,
                Channel          = channel,
                RelatedBookingId = dto.RelatedBookingId,
                IsRead           = false,
                SentAt           = DateTime.UtcNow
            };

            var created = await _notificationRepository.CreateAsync(notification);
            results.Add(MapToDto(created));
        }

        _logger.LogInformation(
            "Bulk notification sent to {Count} recipients. Type={Type}",
            dto.RecipientIds.Count, type);

        return results;
    }

    // ── CRUD Operations ───────────────────────────────────────────────────────

    public async Task<NotificationResponseDto> MarkAsReadAsync(int notificationId)
    {
        var notification = await _notificationRepository
            .FindByNotificationIdAsync(notificationId)
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
        _ = await _notificationRepository.FindByNotificationIdAsync(notificationId)
            ?? throw new KeyNotFoundException($"Notification {notificationId} not found.");

        await _notificationRepository.DeleteByNotificationIdAsync(notificationId);
    }

    public async Task<IList<NotificationResponseDto>> GetAllAsync()
    {
        var notifications = await _notificationRepository.FindAllAsync();
        return notifications.Select(MapToDto).ToList();
    }

    // ── Email (MailKit) ───────────────────────────────────────────────────────

    public async Task SendEmailAsync(
        string toEmail, string toName, string subject, string body)
    {
        var smtpHost    = _configuration["Email:SmtpHost"]    ?? string.Empty;
        var smtpPort    = _configuration.GetValue<int>("Email:SmtpPort", 587);
        var senderEmail = _configuration["Email:SenderEmail"] ?? string.Empty;
        var senderName  = _configuration["Email:SenderName"]  ?? "SkyBooker";
        var password    = _configuration["Email:Password"]    ?? string.Empty;

        // Skip gracefully when email is not yet configured (dev/eval)
        if (string.IsNullOrEmpty(smtpHost) || password == "YOUR_EMAIL_APP_PASSWORD"
            || string.IsNullOrEmpty(password))
        {
            _logger.LogWarning(
                "Email not configured — skipping dispatch to {Email}", toEmail);
            return;
        }

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(senderName, senderEmail));
        email.To.Add(new MailboxAddress(
            string.IsNullOrEmpty(toName) ? toEmail : toName, toEmail));
        email.Subject = subject;
        email.Body    = new TextPart(TextFormat.Html) { Text = body };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(senderEmail, password);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);

        _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
    }

    // ── SMS (Twilio) ──────────────────────────────────────────────────────────

    // ✅ FIX: was using sync MessageResource.Create — now properly async
    public async Task SendSmsAsync(string toPhone, string message)
    {
        var accountSid = _configuration["Twilio:AccountSid"] ?? string.Empty;
        var authToken  = _configuration["Twilio:AuthToken"]  ?? string.Empty;
        var fromNumber = _configuration["Twilio:FromNumber"] ?? string.Empty;

        // Skip gracefully when Twilio is not yet configured (dev/eval)
        if (string.IsNullOrEmpty(accountSid)
            || accountSid == "YOUR_TWILIO_ACCOUNT_SID")
        {
            _logger.LogWarning(
                "Twilio not configured — skipping SMS dispatch to {Phone}", toPhone);
            return;
        }

        TwilioClient.Init(accountSid, authToken);

        await MessageResource.CreateAsync(
            body: message,
            from: new Twilio.Types.PhoneNumber(fromNumber),
            to:   new Twilio.Types.PhoneNumber(toPhone)
        );

        _logger.LogInformation("SMS sent to {Phone}", toPhone);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Sends the booking confirmation email with:
    /// - Beautiful HTML body
    /// - QuestPDF e-ticket attached as PDF
    /// </summary>
    private async Task SendBookingConfirmationEmailAsync(BookingConfirmationRequestDto dto)
    {
        var apiKey      = _configuration["SendGrid:ApiKey"] ?? string.Empty;
        var senderEmail = _configuration["Email:SenderEmail"] ?? string.Empty;
        var senderName  = _configuration["Email:SenderName"] ?? "SkyBooker";

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning(
                "SendGrid not configured — skipping confirmation email for PNR={Pnr}", dto.PnrCode);
            return;
        }

        var pdfBytes = GenerateETicketPdf(dto);

        var client  = new SendGridClient(apiKey);
        var from    = new SendGrid.Helpers.Mail.EmailAddress(senderEmail, senderName);
        var to      = new SendGrid.Helpers.Mail.EmailAddress(dto.RecipientEmail, dto.RecipientName);
        var subject = $"✈ Booking Confirmed! PNR: {dto.PnrCode} | SkyBooker";
        var htmlContent = BuildBookingConfirmationHtml(dto);

        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: null, htmlContent);

        // Attach PDF e-ticket
        var pdfBase64 = Convert.ToBase64String(pdfBytes);
        msg.AddAttachment(
            filename:    $"eticket_{dto.PnrCode}.pdf",
            base64Content: pdfBase64,
            type:        "application/pdf",
            disposition: "attachment"
        );

        var response = await client.SendEmailAsync(msg);

        if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
        {
            _logger.LogInformation(
                "Booking confirmation email sent to {Email}, PNR={Pnr}",
                dto.RecipientEmail, dto.PnrCode);
        }
        else
        {
            var body = await response.Body.ReadAsStringAsync();
            _logger.LogWarning(
                "SendGrid failed for PNR={Pnr}, Status={Status}, Body={Body}",
                dto.PnrCode, response.StatusCode, body);
        }
    }

    /// <summary>Generates a clean e-ticket PDF using QuestPDF</summary>
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
                                .FontSize(12).FontColor(Colors.Grey.Darken1);
                        });
                        row.AutoItem().Column(right =>
                        {
                            right.Item().AlignRight()
                                .Text("PNR").FontSize(10).FontColor(Colors.Grey.Medium);
                            right.Item().AlignRight().Text(dto.PnrCode)
                                .FontSize(22).Bold().FontColor(Colors.Blue.Darken2);
                        });
                    });
                    col.Item().PaddingTop(8).LineHorizontal(2).LineColor(Colors.Blue.Darken2);
                });

                page.Content().PaddingTop(20).Column(col =>
                {
                    // Passenger info bar
                    col.Item().Background(Colors.Blue.Lighten5).Padding(14).Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("Passenger")
                                .FontSize(9).FontColor(Colors.Grey.Medium);
                            left.Item().Text(dto.RecipientName).Bold().FontSize(14);
                        });
                        row.RelativeItem().Column(mid =>
                        {
                            mid.Item().Text("Seat")
                                .FontSize(9).FontColor(Colors.Grey.Medium);
                            mid.Item().Text(dto.SeatNumber).Bold().FontSize(14);
                        });
                        row.RelativeItem().Column(right =>
                        {
                            right.Item().Text("Flight")
                                .FontSize(9).FontColor(Colors.Grey.Medium);
                            right.Item().Text(dto.FlightNumber).Bold().FontSize(14);
                        });
                    });

                    // Route
                    col.Item().PaddingTop(20).Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("FROM").FontSize(9)
                                .FontColor(Colors.Grey.Medium);
                            left.Item().Text(dto.Origin).FontSize(28).Bold()
                                .FontColor(Colors.Blue.Darken3);
                        });
                        row.AutoItem().PaddingHorizontal(16).AlignMiddle()
                            .Text("✈").FontSize(22).FontColor(Colors.Blue.Medium);
                        row.RelativeItem().Column(right =>
                        {
                            right.Item().Text("TO").FontSize(9)
                                .FontColor(Colors.Grey.Medium);
                            right.Item().Text(dto.Destination).FontSize(28).Bold()
                                .FontColor(Colors.Blue.Darken3);
                        });
                    });

                    // Trip details table
                    col.Item().PaddingTop(20).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn();
                            cols.RelativeColumn();
                            cols.RelativeColumn();
                        });

                        table.Cell().Padding(10).Column(c =>
                        {
                            c.Item().Text("Departure Date")
                                .FontSize(9).FontColor(Colors.Grey.Medium);
                            c.Item().Text(dto.DepartureTime.ToString("dd MMM yyyy")).Bold();
                        });
                        table.Cell().Padding(10).Column(c =>
                        {
                            c.Item().Text("Departure Time")
                                .FontSize(9).FontColor(Colors.Grey.Medium);
                            c.Item().Text(dto.DepartureTime.ToString("HH:mm")).Bold()
                                .FontSize(18).FontColor(Colors.Blue.Darken2);
                        });
                        table.Cell().Padding(10).Column(c =>
                        {
                            c.Item().Text("Total Fare")
                                .FontSize(9).FontColor(Colors.Grey.Medium);
                            c.Item().Text($"₹{dto.TotalFare:F2}").Bold()
                                .FontColor(Colors.Green.Darken3).FontSize(16);
                        });
                    });

                    col.Item().PaddingTop(20).LineHorizontal(1)
                        .LineColor(Colors.Grey.Lighten2);

                    col.Item().PaddingTop(10).Background(Colors.Grey.Lighten4)
                        .Padding(12).Text(
                            "Please arrive at the airport at least 2 hours before departure. " +
                            "This e-ticket is valid as your travel document. " +
                            "Web check-in opens 24 hours before departure.")
                        .FontSize(9).FontColor(Colors.Grey.Darken1).Italic();
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("SkyBooker Platform  •  ")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                    text.Span("Search. Book. Fly. Effortlessly.")
                        .FontSize(9).FontColor(Colors.Grey.Medium).Italic();
                });
            });
        }).GeneratePdf();
    }

    /// <summary>Builds a polished HTML email body for booking confirmation</summary>
    private static string BuildBookingConfirmationHtml(BookingConfirmationRequestDto dto) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8"/>
          <meta name="viewport" content="width=device-width,initial-scale=1.0"/>
          <title>Booking Confirmed</title>
        </head>
        <body style="margin:0;padding:0;background:#f0f4f8;font-family:'Segoe UI',Arial,sans-serif;">
          <div style="max-width:600px;margin:32px auto;background:#fff;border-radius:12px;
                      overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);">

            <!-- Header -->
            <div style="background:#1d4ed8;padding:28px 40px;text-align:center;">
              <div style="font-size:26px;font-weight:700;color:#fff;">✈ SkyBooker</div>
              <div style="font-size:16px;color:#bfdbfe;margin-top:6px;">Booking Confirmed!</div>
            </div>

            <!-- Body -->
            <div style="padding:32px 40px;">
              <p style="color:#374151;font-size:16px;">Hi <strong>{dto.RecipientName}</strong>,</p>
              <p style="color:#4b5563;font-size:15px;">
                Your booking is confirmed. Your e-ticket is attached to this email as a PDF.
              </p>

              <!-- PNR Badge -->
              <div style="background:#1d4ed8;border-radius:8px;padding:16px 24px;
                          text-align:center;margin:20px 0;">
                <div style="font-size:11px;color:#bfdbfe;letter-spacing:1px;">BOOKING REFERENCE</div>
                <div style="font-size:26px;font-weight:700;color:#fff;
                            letter-spacing:3px;margin-top:4px;">{dto.PnrCode}</div>
              </div>

              <!-- Route -->
              <div style="text-align:center;padding:20px;background:#eff6ff;
                          border-radius:8px;margin:16px 0;">
                <span style="font-size:28px;font-weight:700;color:#1d4ed8;">{dto.Origin}</span>
                <span style="font-size:18px;color:#93c5fd;margin:0 12px;">✈</span>
                <span style="font-size:28px;font-weight:700;color:#1d4ed8;">{dto.Destination}</span>
                <div style="font-size:12px;color:#6b7280;margin-top:4px;">{dto.FlightNumber}</div>
              </div>

              <!-- Details Table -->
              <table width="100%" cellpadding="10" cellspacing="0"
                     style="border:1px solid #e5e7eb;border-radius:8px;overflow:hidden;
                            border-collapse:separate;font-size:14px;">
                <tr style="background:#f9fafb;">
                  <td style="color:#6b7280;border-bottom:1px solid #e5e7eb;">Departure</td>
                  <td style="font-weight:600;color:#111827;border-bottom:1px solid #e5e7eb;">
                    {dto.DepartureTime:ddd, dd MMM yyyy · HH:mm}
                  </td>
                </tr>
                <tr>
                  <td style="color:#6b7280;border-bottom:1px solid #e5e7eb;">Seat</td>
                  <td style="font-weight:600;color:#111827;border-bottom:1px solid #e5e7eb;">
                    {dto.SeatNumber}
                  </td>
                </tr>
                <tr style="background:#f9fafb;">
                  <td style="color:#6b7280;">Total Fare</td>
                  <td style="font-weight:700;color:#059669;font-size:18px;">₹{dto.TotalFare:F2}</td>
                </tr>
              </table>

              <p style="color:#6b7280;font-size:13px;margin-top:20px;">
                ✅ Arrive at the airport at least <strong>2 hours</strong> before departure.<br/>
                ✅ Web check-in opens <strong>24 hours</strong> before your flight.<br/>
                ✅ Carry a valid photo ID and this booking reference.
              </p>
            </div>

            <!-- Footer -->
            <div style="background:#f9fafb;border-top:1px solid #e5e7eb;
                        padding:20px 40px;text-align:center;">
              <p style="color:#9ca3af;font-size:12px;margin:0;">
                SkyBooker • Search. Book. Fly. Effortlessly.
              </p>
              <p style="color:#9ca3af;font-size:11px;margin:4px 0 0;">
                This is an automated email. Please do not reply.
              </p>
            </div>
          </div>
        </body>
        </html>
        """;

    /// <summary>Simple HTML wrapper for generic notifications</summary>
    private static string BuildSimpleHtml(string title, string message) => $"""
        <!DOCTYPE html>
        <html><body style="font-family:'Segoe UI',Arial,sans-serif;max-width:600px;
                           margin:32px auto;background:#f9fafb;padding:24px;border-radius:8px;">
          <div style="background:#1d4ed8;padding:16px 24px;border-radius:8px 8px 0 0;">
            <span style="color:#fff;font-size:18px;font-weight:700;">✈ SkyBooker</span>
          </div>
          <div style="background:#fff;padding:24px;border:1px solid #e5e7eb;
                      border-top:none;border-radius:0 0 8px 8px;">
            <h2 style="color:#111827;margin:0 0 12px;">{title}</h2>
            <p style="color:#4b5563;font-size:15px;line-height:1.6;">{message}</p>
          </div>
        </body></html>
        """;

    // ── Mapper ────────────────────────────────────────────────────────────────

    private static NotificationResponseDto MapToDto(Notification n) => new()
    {
        NotificationId   = n.NotificationId,
        RecipientId      = n.RecipientId,
        Type             = n.Type.ToString(),
        Title            = n.Title,
        Message          = n.Message,
        Channel          = n.Channel.ToString(),
        RelatedBookingId = n.RelatedBookingId,
        IsRead           = n.IsRead,
        SentAt           = n.SentAt
    };
}