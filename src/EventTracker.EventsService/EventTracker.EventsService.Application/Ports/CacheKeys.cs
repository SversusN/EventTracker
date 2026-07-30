namespace EventTracker.EventsService.Application.Ports;

/// <summary>
/// Централизованные ключи кеша сервиса событий.
/// </summary>
public static class CacheKeys
{
    public const string TopEvents = "events:top10";

    public static string Event(Guid eventId) => $"event:{eventId}";
}
