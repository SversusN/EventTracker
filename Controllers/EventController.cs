using EventTrackerApi.Infrastructure.Mappers;
using EventTrackerApi.Models.Dto;
using EventTrackerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventTrackerApi.Controllers;

/// <summary>
/// Контроллер для управления событиями (мероприятиями)
/// </summary>
[ApiController]
[Route("events")]
public class EventsController(IEventService eventService) : ControllerBase
{
    /// <summary>
    /// Получить список всех событий
    /// </summary>
    /// <returns>Список событий</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventResponseDto>), StatusCodes.Status200OK)]
    public IActionResult GetAllEvents()
    {
        var events = eventService.GetAllEvents();
        return Ok(EventMapper.ToResponseDtoList(events));
    }

    /// <summary>
    /// Получить событие по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор события (GUID)</param>
    /// <returns>Событие с указанным идентификатором</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetEventById(Guid id)
    {
        var ev = eventService.GetEventById(id);
        if (ev is null)
        {
            return NotFound();
        }
        return Ok(EventMapper.ToResponseDto(ev));
    }

    /// <summary>
    /// Создать новое событие
    /// </summary>
    /// <param name="dto">Данные для создания события</param>
    /// <returns>Созданное событие с идентификатором</returns>
    [HttpPost]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult CreateEvent([FromBody] CreateEventDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var createdEvent = eventService.CreateEvent(dto);
        return CreatedAtAction(nameof(GetEventById), new { id = createdEvent.Id }, EventMapper.ToResponseDto(createdEvent));
    }

    /// <summary>
    /// Обновить существующее событие
    /// </summary>
    /// <param name="id">Идентификатор события для обновления</param>
    /// <param name="dto">Новые данные события</param>
    /// <returns>Обновленное событие</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult UpdateEvent(Guid id, [FromBody] UpdateEventDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var updatedEvent = eventService.UpdateEvent(id, dto);
        if (updatedEvent is null)
        {
            return NotFound();
        }
        return Ok(EventMapper.ToResponseDto(updatedEvent));
    }

    /// <summary>
    /// Удалить событие по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор события для удаления</param>
    /// <returns>Результат удаления</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteEvent(Guid id)
    {
        var deleted = eventService.DeleteEvent(id);
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }
}
