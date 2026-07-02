using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventTracker.BookingsService.Presentation.Infrastructure;

public static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
}
