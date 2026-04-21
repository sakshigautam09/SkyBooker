namespace SkyBooker.SeatService.DTOs;

public class UpdateSeatRequestDto
{
    public bool? IsWindow { get; set; }
    public bool? IsAisle { get; set; }
    public bool? HasExtraLegroom { get; set; }
    public decimal? PriceMultiplier { get; set; }
    public string? SeatClass { get; set; }
}
