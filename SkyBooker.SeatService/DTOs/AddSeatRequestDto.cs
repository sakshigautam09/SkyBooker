namespace SkyBooker.SeatService.DTOs;

public class AddSeatRequestDto
{
    public int FlightId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string SeatClass { get; set; } = string.Empty;
    public int Row { get; set; }
    public string Column { get; set; } = string.Empty;
    public bool IsWindow { get; set; }
    public bool IsAisle { get; set; }
    public bool HasExtraLegroom { get; set; }
    public decimal PriceMultiplier { get; set; } = 1.0m;
}
