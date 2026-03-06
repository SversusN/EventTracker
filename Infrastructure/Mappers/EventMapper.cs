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
    /// Создаёт новый Event из CreateEventDto
    /// </summary>
    public static Event FromCreateDto(CreateEventDto dto)
    {
        return new Event(
            dto.Title,
            dto.Description,
            dto.StartAt,
            dto.EndAt
        );
    }

    /// <summary>
    /// Создаёт Event из UpdateEventDto с указанным Id
    /// </summary>
    public static Event FromUpdateDto(Guid id, UpdateEventDto dto)
    {
        return new Event(
            id,
            dto.Title,
            dto.Description,
            dto.StartAt,
            dto.EndAt
        );
    }
}
