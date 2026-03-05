using EventTracker.Models;
using EventTracker.Models.Dto;
using EventTracker.Models.Dtos;

namespace EventTracker.Services;

public interface IEventService
{
    IEnumerable<EventResponseDto> GetAllEvents();
    EventResponseDto? GetEventById(Guid id);
    EventResponseDto CreateEvent(CreateEventDto dto);
    EventResponseDto? UpdateEvent(Guid id, UpdateEventDto dto);
    bool DeleteEvent(Guid id);
}