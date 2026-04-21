using SkyBooker.BookingService.Context;
using SkyBooker.BookingService.DTOs;
using SkyBooker.BookingService.Entities;
using SkyBooker.BookingService.Enums;
using SkyBooker.BookingService.Repositories;

namespace SkyBooker.BookingService.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly BookingDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IBookingRepository bookingRepository,
        BookingDbContext context,
        IConfiguration configuration,
        ILogger<BookingService> logger)
    {
        _bookingRepository = bookingRepository;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<BookingResponseDto> CreateBookingAsync(CreateBookingRequestDto dto)
    {
        // ── Validation ────────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(dto.ContactEmail))
            throw new ArgumentException("Contact email is required.");

        if (dto.FlightId <= 0)
            throw new ArgumentException("Valid FlightId is required.");

        if (!Enum.TryParse<TripType>(dto.TripType, true, out var tripType))
            throw new ArgumentException($"Invalid trip type: {dto.TripType}. Must be OneWay or RoundTrip.");

        // ReturnFlightId is required for RoundTrip, must be null for OneWay
        if (tripType == TripType.RoundTrip && dto.ReturnFlightId == null)
            throw new ArgumentException("ReturnFlightId is required for RoundTrip bookings.");

        // For OneWay, always clear returnFlightId even if user sent one by mistake
        if (tripType == TripType.OneWay)
            dto.ReturnFlightId = null;

        // ── Fare Calculation ──────────────────────────────────────────────────
        var fareSummary = await CalculateFareAsync(new CalculateFareRequestDto
        {
            BasePrice = dto.BasePrice,
            PassengerCount = dto.SeatIds.Count > 0 ? dto.SeatIds.Count : 1,
            LuggageKg = dto.LuggageKg,
            HasMealPreference = !string.IsNullOrEmpty(dto.MealPreference)
        });

        // ── PNR Generation ────────────────────────────────────────────────────
        var pnr = await GeneratePnrAsync();

        var booking = new Booking
        {
            BookingId = Guid.NewGuid().ToString(),
            UserId = dto.UserId,
            FlightId = dto.FlightId,
            ReturnFlightId = dto.ReturnFlightId,
            PnrCode = pnr,
            TripType = tripType,
            Status = BookingStatus.Pending,
            BaseFare = fareSummary.BaseFare,
            Taxes = fareSummary.Taxes,
            AncillaryCost = fareSummary.AncillaryCost,
            TotalFare = fareSummary.TotalFare,
            MealPreference = dto.MealPreference,
            LuggageKg = dto.LuggageKg,
            ContactEmail = dto.ContactEmail,
            ContactPhone = dto.ContactPhone,
            SeatIds = string.Join(",", dto.SeatIds),
            BookedAt = DateTime.UtcNow
        };

        // EF Core transaction — atomically persist booking
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var created = await _bookingRepository.CreateAsync(booking);
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Booking created: PNR={Pnr}, UserId={UserId}, FlightId={FlightId}",
                pnr, dto.UserId, dto.FlightId);

            return MapToDto(created);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Booking creation failed for UserId={UserId}", dto.UserId);
            throw;
        }
    }

    public async Task<BookingResponseDto?> GetBookingByIdAsync(string bookingId)
    {
        var booking = await _bookingRepository.FindByBookingIdAsync(bookingId);
        return booking == null ? null : MapToDto(booking);
    }

    public async Task<BookingResponseDto?> GetBookingByPnrAsync(string pnrCode)
    {
        var booking = await _bookingRepository.FindByPnrCodeAsync(pnrCode);
        return booking == null ? null : MapToDto(booking);
    }

    public async Task<IList<BookingResponseDto>> GetBookingsByUserAsync(int userId)
    {
        var bookings = await _bookingRepository.FindByUserIdAsync(userId);
        return bookings.Select(MapToDto).ToList();
    }

    public async Task<IList<BookingResponseDto>> GetBookingsByFlightAsync(int flightId)
    {
        var bookings = await _bookingRepository.FindByFlightIdAsync(flightId);
        return bookings.Select(MapToDto).ToList();
    }

    public async Task<BookingResponseDto> CancelBookingAsync(string bookingId)
    {
        var booking = await _bookingRepository.FindByBookingIdAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        if (booking.Status == BookingStatus.Cancelled)
            throw new InvalidOperationException("Booking is already cancelled.");

        if (booking.Status == BookingStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed booking.");

        booking.Status = BookingStatus.Cancelled;
        var updated = await _bookingRepository.UpdateAsync(booking);

        _logger.LogInformation("Booking cancelled: PNR={Pnr}", booking.PnrCode);
        return MapToDto(updated);
    }

    public async Task<BookingResponseDto> UpdateStatusAsync(string bookingId, UpdateBookingStatusDto dto)
    {
        var booking = await _bookingRepository.FindByBookingIdAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        if (!Enum.TryParse<BookingStatus>(dto.Status, true, out var newStatus))
            throw new ArgumentException($"Invalid booking status: {dto.Status}");

        booking.Status = newStatus;

        if (!string.IsNullOrEmpty(dto.PaymentId))
            booking.PaymentId = dto.PaymentId;

        var updated = await _bookingRepository.UpdateAsync(booking);
        return MapToDto(updated);
    }

    public Task<FareSummary> CalculateFareAsync(CalculateFareRequestDto dto)
    {
        var taxRate = _configuration.GetValue<double>("Fare:TaxRatePercent", 18.0) / 100.0;
        var baggageRate = _configuration.GetValue<double>("Fare:BaggageRatePerKg", 250.0);

        var baseFare = dto.BasePrice * dto.PassengerCount;
        var taxes = baseFare * (decimal)taxRate;

        // Ancillary: extra baggage + meal surcharge
        var baggageCost = dto.LuggageKg > 15
            ? (dto.LuggageKg - 15) * (decimal)baggageRate
            : 0;

        var mealCost = dto.HasMealPreference ? 350m * dto.PassengerCount : 0;
        var ancillaryCost = baggageCost + mealCost;

        var totalFare = baseFare + taxes + ancillaryCost;

        return Task.FromResult(new FareSummary(
            BaseFare: Math.Round(baseFare, 2),
            Taxes: Math.Round(taxes, 2),
            AncillaryCost: Math.Round(ancillaryCost, 2),
            TotalFare: Math.Round(totalFare, 2)
        ));
    }

    public async Task<BookingResponseDto> AddAddOnAsync(string bookingId, AddAddOnRequestDto dto)
    {
        var booking = await _bookingRepository.FindByBookingIdAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        if (booking.Status == BookingStatus.Cancelled
            || booking.Status == BookingStatus.Completed)
            throw new InvalidOperationException("Cannot add add-ons to a cancelled or completed booking.");

        if (dto.AddOnType.Equals("Meal", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrEmpty(dto.MealPreference))
        {
            booking.MealPreference = dto.MealPreference;
        }

        if (dto.AddOnType.Equals("Baggage", StringComparison.OrdinalIgnoreCase)
            && dto.ExtraBaggageKg.HasValue)
        {
            booking.LuggageKg += dto.ExtraBaggageKg.Value;
        }

        booking.AncillaryCost += dto.Cost;
        booking.TotalFare += dto.Cost;

        var updated = await _bookingRepository.UpdateAsync(booking);
        return MapToDto(updated);
    }

    public async Task<IList<BookingResponseDto>> GetUpcomingBookingsAsync(int userId)
    {
        // Upcoming = Pending (awaiting payment) + Confirmed (paid)
        // Excludes Cancelled, Completed, NoShow
        var confirmed = await _bookingRepository.FindByUserIdAndStatusAsync(
            userId, BookingStatus.Confirmed.ToString());

        var pending = await _bookingRepository.FindByUserIdAndStatusAsync(
            userId, BookingStatus.Pending.ToString());

        return confirmed.Concat(pending)
            .OrderByDescending(b => b.BookedAt)
            .Select(MapToDto)
            .ToList();
    }

    // ─── PNR Generation ───────────────────────────────────────────────────────
    private async Task<string> GeneratePnrAsync()
    {
        string pnr;
        int attempts = 0;
        const int maxAttempts = 10;

        do
        {
            // 6-char alphanumeric from Guid — exactly as specified in case study
            pnr = Guid.NewGuid().ToString("N")[..6].ToUpper();
            attempts++;

            if (attempts >= maxAttempts)
                throw new InvalidOperationException("Failed to generate unique PNR after max attempts.");
        }
        while (await _bookingRepository.ExistsByPnrCodeAsync(pnr));

        return pnr;
    }

    // ─── Mapper ───────────────────────────────────────────────────────────────
    private static BookingResponseDto MapToDto(Booking b) => new()
    {
        BookingId = b.BookingId,
        UserId = b.UserId,
        FlightId = b.FlightId,
        ReturnFlightId = b.ReturnFlightId,
        PnrCode = b.PnrCode,
        TripType = b.TripType.ToString(),
        Status = b.Status.ToString(),
        BaseFare = b.BaseFare,
        Taxes = b.Taxes,
        AncillaryCost = b.AncillaryCost,
        TotalFare = b.TotalFare,
        MealPreference = b.MealPreference,
        LuggageKg = b.LuggageKg,
        ContactEmail = b.ContactEmail,
        ContactPhone = b.ContactPhone,
        PaymentId = b.PaymentId,
        BookedAt = b.BookedAt,
        SeatIds = string.IsNullOrEmpty(b.SeatIds)
            ? new List<int>()
            : b.SeatIds.Split(',').Select(int.Parse).ToList()
    };
}
