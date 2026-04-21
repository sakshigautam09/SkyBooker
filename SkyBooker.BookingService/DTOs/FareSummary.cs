namespace SkyBooker.BookingService.DTOs;

// Immutable record type as specified in the case study
public record FareSummary(
    decimal BaseFare,
    decimal Taxes,
    decimal AncillaryCost,
    decimal TotalFare
);
