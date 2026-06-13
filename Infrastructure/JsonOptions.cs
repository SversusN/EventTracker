using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventTrackerApi.Presentation.Infrastructure;

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
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
}
