namespace EventTrackerApi.Application.Options;

public class BookingOptions
{
    public const string SectionName = "Booking";

    public int MaxActiveBookingsPerUser { get; set; } = 10;
}
