using EventTracker.EventsService.Application.DTOs;
using EventTracker.EventsService.Application.Mappers;
using EventTracker.EventsService.Application.Services;
using EventTracker.EventsService.Presentation.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventTracker.EventsService.Presentation.Controllers;

/// <summary>
/// Контроллер для управления событиями (мероприятиями)
/// </summary>
[ApiController]
[Route("events")]
public class EventsController(IEventService eventService) : ControllerBase
{
    private readonly IEventService _eventService = eventService;

    /// <summary>
    /// Получить список событий с фильтрацией и пагинацией
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<EventResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEvents(
        [FromQuery] string? title = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1)
        {
            return BadRequest(ProblemDetailsHelper.InvalidPageNumber());
        }

        if (pageSize < 1)
        {
            return BadRequest(ProblemDetailsHelper.InvalidPageSize());
        }

        var result = await _eventService.GetEventsAsync(title, from, to, page, pageSize);

        var response = new PaginatedResult<EventResponseDto>
        {
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            Items = EventMapper.ToResponseDtoList(result.Items)
        };

        return Ok(response);
    }

    /// <summary>
    /// Получить событие по идентификатору
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventById(Guid id)
    {
        var ev = await _eventService.GetEventByIdAsync(id);
        if (ev is null)
        {
            return NotFound(ProblemDetailsHelper.NotFound("Событие", id));
        }
        return Ok(EventMapper.ToResponseDto(ev));
    }

    /// <summary>
    /// Создать новое событие (только для администраторов)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var createdEvent = await _eventService.CreateEventAsync(dto.Title, dto.Description, dto.StartAt, dto.EndAt, dto.TotalSeats);
        return CreatedAtAction(nameof(GetEventById), new { id = createdEvent.Id }, EventMapper.ToResponseDto(createdEvent));
    }

    /// <summary>
    /// Обновить существующее событие (только для администраторов)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventDto dto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var updatedEvent = await _eventService.UpdateEventAsync(id, dto.Title, dto.Description, dto.StartAt, dto.EndAt);
        if (updatedEvent is null)
        {
            return NotFound(ProblemDetailsHelper.NotFound("Событие", id));
        }
        return Ok(EventMapper.ToResponseDto(updatedEvent));
    }

    /// <summary>
    /// Удалить событие по идентификатору (только для администраторов)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteEvent(Guid id)
    {
        var deleted = await _eventService.DeleteEventAsync(id);
        if (!deleted)
        {
            return NotFound(ProblemDetailsHelper.NotFound("Событие", id));
        }
        return NoContent();
    }
}
