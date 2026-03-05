using EventTracker.Models;
using EventTracker.Models.Dto;
using EventTracker.Models.Dtos;

namespace EventTracker.Services;

public class EventService : IEventService
{
    private readonly List<Event> _events = [];


    public IEnumerable<EventResponseDto> GetAllEvents()
    {
         return _events.Select(MapToResponseDto).ToList();
    }

    public EventResponseDto? GetEventById(Guid id)
    {
            var ev = _events.FirstOrDefault(e => e.Id == id);
            return ev is null ? null : MapToResponseDto(ev);
    }

    public EventResponseDto CreateEvent(CreateEventDto dto)
    {
        var ev = new Event(
            dto.Title,
            dto.Description,
            dto.StartAt,
            dto.EndAt
        );

            _events.Add(ev);
        return MapToResponseDto(ev);
    }

    public EventResponseDto? UpdateEvent(Guid id, UpdateEventDto dto)
    {
            var ev = _events.FirstOrDefault(e => e.Id == id);
            if (ev is null)
            {
                return null;
            }

            ev.Update(
                dto.Title,
                dto.Description,
                dto.StartAt,
                dto.EndAt
            );

            return MapToResponseDto(ev);
     }
    

    public bool DeleteEvent(Guid id)
    {
            var ev = _events.FirstOrDefault(e => e.Id == id);
            if (ev is null)
            {
                return false;
            }
            _events.Remove(ev);
            return true;
        }
   
    //маппиннг в ответ
    private static EventResponseDto MapToResponseDto(Event ev)
    {
        return new EventResponseDto(
            ev.Id,
            ev.Title,
            ev.Description,
            ev.StartAt,
            ev.EndAt
        );
    }
}

