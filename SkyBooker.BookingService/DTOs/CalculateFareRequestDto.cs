namespace SkyBooker.BookingService.DTOs;

public class CalculateFareRequestDto
{
    public decimal BasePrice { get; set; }
    public int PassengerCount { get; set; } = 1;
    public int LuggageKg { get; set; } = 0;
    public bool HasMealPreference { get; set; } = false;
}
