using EventTrackerApi.Models;
using EventTrackerApi.Models.Dto;

namespace EventTrackerApi.Infrastructure.Mappers;

/// <summary>
/// Маппер для преобразования между Event и DTO
/// </summary>
public static class EventMapper
{
    /// <summary>
    /// Преобразует Event в EventResponseDto
    /// </summary>
    public static EventResponseDto ToResponseDto(Event ev)
    {
        return new EventResponseDto(
            ev.Id,
            ev.Title,
            ev.Description,
            ev.StartAt,
            ev.EndAt
        );
    }

    /// <summary>
    /// Преобразует коллекцию Event в коллекцию EventResponseDto
    /// </summary>
    public static IEnumerable<EventResponseDto> ToResponseDtoList(IEnumerable<Event> events)
    {
        return events.Select(ToResponseDto);
    }

    /// <summary>
    /// Создаёт новый Event из параметров
    /// </summary>
    public static Event FromCreateDto(string title, string? description, DateTime startAt, DateTime endAt)
    {
        return new Event(
            title,
            description,
            startAt,
            endAt
        );
    }

    /// <summary>
    /// Создаёт Event с указанным Id из параметров
    /// </summary>
    public static Event FromUpdateDto(Guid id, string title, string? description, DateTime startAt, DateTime endAt)
    {
        return new Event(
            id,
            title,
            description,
            startAt,
            endAt
        );
    }
}
