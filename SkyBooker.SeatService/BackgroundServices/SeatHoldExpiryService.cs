using SkyBooker.SeatService.Context;
using SkyBooker.SeatService.Enums;
using SkyBooker.SeatService.Repositories;

namespace SkyBooker.SeatService.BackgroundServices;

public class SeatHoldExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SeatHoldExpiryService> _logger;

    public SeatHoldExpiryService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<SeatHoldExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SeatHoldExpiryService started.");

        var scanIntervalSeconds = _configuration.GetValue<int>("SeatHold:ScanIntervalSeconds", 120);
        var expiryMinutes = _configuration.GetValue<int>("SeatHold:ExpiryMinutes", 15);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReleaseExpiredSeatsAsync(expiryMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing expired seat holds.");
            }

            await Task.Delay(TimeSpan.FromSeconds(scanIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("SeatHoldExpiryService stopped.");
    }

    private async Task ReleaseExpiredSeatsAsync(int expiryMinutes)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISeatRepository>();

        var expiryThreshold = DateTime.UtcNow.AddMinutes(-expiryMinutes);
        var expiredSeats = await repository.FindHeldSeatsExpiredAsync(expiryThreshold);

        if (expiredSeats.Count == 0) return;

        _logger.LogInformation(
            "Releasing {Count} expired seat holds older than {Minutes} minutes.",
            expiredSeats.Count, expiryMinutes);

        foreach (var seat in expiredSeats)
        {
            seat.Status = SeatStatus.Available;
            seat.HeldSince = null;
            seat.HeldByUserId = null;
            await repository.UpdateAsync(seat);
        }

        _logger.LogInformation("Released {Count} expired seat holds.", expiredSeats.Count);
    }
}
