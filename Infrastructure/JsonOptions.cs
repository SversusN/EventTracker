using System.Text.Json;

namespace EventTrackerApi.Infrastructure;

/// <summary>
/// Общие настройки JSON сериализации для всего приложения
/// </summary>
public static class JsonOptions
{
    /// <summary>
    /// Стандартные настройки JSON (camelCase)
    /// </summary>
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
