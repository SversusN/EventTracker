using EventTracker.Models.Dto;
using EventTracker.Models.Dtos;
using EventTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventTracker.Controllers;


[ApiController]
[Route("events")]

public class EventController(IEventService eventService) : ControllerBase
{
    private readonly IEventService _eventService = eventService;

    [HttpGet]
    public IActionResult GetAllEvents()
    {
        var events = _eventService.GetAllEvents();
        return Ok(events);
    }

    [HttpGet("{id}")]
    public IActionResult GetEventById(Guid id)
    {
        var singleEvent = _eventService.GetEventById(id);
        if (singleEvent is null)
        {
            return NotFound();
        }
        return Ok(singleEvent);
    }

    [HttpPost]

    public IActionResult CreateEvent([FromBody] CreateEventDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        var createEvent = _eventService.CreateEvent(dto);
        return CreatedAtAction(nameof(GetEventById), new { id = createEvent.Id }, createEvent);
    }

    [HttpPut("id")]
    public IActionResult UpdateEvevnt(Guid id, [FromBody] UpdateEventDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        var updateEvent = _eventService.UpdateEvent(id, dto);
        if (updateEvent is null)
        {
            return NotFound();
        }
        return Ok(updateEvent);
    }

    [HttpDelete("{id:guid}")]

    public IActionResult DeleteEvent(Guid id)
    {
        var deleted = _eventService.DeleteEvent(id);
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }
}