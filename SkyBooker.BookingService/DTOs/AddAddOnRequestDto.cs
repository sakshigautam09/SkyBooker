namespace SkyBooker.BookingService.DTOs;

public class AddAddOnRequestDto
{
    public string AddOnType { get; set; } = string.Empty; // "Baggage" or "Meal"
    public string? MealPreference { get; set; }           // Veg / NonVeg / Jain / Vegan
    public int? ExtraBaggageKg { get; set; }
    public decimal Cost { get; set; }
}
