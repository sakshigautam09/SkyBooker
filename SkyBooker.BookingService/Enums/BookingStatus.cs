namespace SkyBooker.BookingService.Enums;

public enum BookingStatus
{
    Pending,       // payment not completed
    Confirmed,     // payment successful
    Cancelled,     // cancelled by user or airline
    Completed,     // flight departed
    NoShow         // passenger did not check in
}
