namespace EventTracker.EventsService.Application.Options;

/// <summary>
/// Настройки кеширования.
/// </summary>
public class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>
    /// TTL для отдельного события (в секундах).
    /// </summary>
    public int EventTtlSeconds { get; set; } = 300;

    /// <summary>
    /// TTL для топ-10 событий (в секундах).
    /// </summary>
    public int TopEventsTtlSeconds { get; set; } = 60;
}
