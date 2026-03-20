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
    private readonly IEventService _eventService = eventService;

    /// <summary>
    /// Получить список событий с фильтрацией и пагинацией
    /// </summary>
    /// <param name="title">Поиск по названию (частичное совпадение, регистронезависимый)</param>
    /// <param name="from">События, начинающиеся не раньше указанной даты</param>
    /// <param name="to">События, заканчивающиеся не позже указанной даты</param>
    /// <param name="page">Номер страницы (начиная с 1)</param>
    /// <param name="pageSize">Количество элементов на странице</param>
    /// <returns>Список событий с информацией о пагинации</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<EventResponseDto>), StatusCodes.Status200OK)]
    public IActionResult GetEvents(
        [FromQuery] string? title = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = _eventService.GetEvents(title, from, to, page, pageSize);

        var response = new PaginatedResult<EventResponseDto>
        {
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            Items = Infrastructure.Mappers.EventMapper.ToResponseDtoList(result.Items)
        };

        return Ok(response);
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
        var ev = _eventService.GetEventById(id);
        if (ev is null)
        {
            return NotFound();
        }
        return Ok(Infrastructure.Mappers.EventMapper.ToResponseDto(ev));
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

        var createdEvent = _eventService.CreateEvent(dto.Title, dto.Description, dto.StartAt, dto.EndAt);
        return CreatedAtAction(nameof(GetEventById), new { id = createdEvent.Id }, Infrastructure.Mappers.EventMapper.ToResponseDto(createdEvent));
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

        var updatedEvent = _eventService.UpdateEvent(id, dto.Title, dto.Description, dto.StartAt, dto.EndAt);
        if (updatedEvent is null)
        {
            return NotFound();
        }
        return Ok(Infrastructure.Mappers.EventMapper.ToResponseDto(updatedEvent));
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
        var deleted = _eventService.DeleteEvent(id);
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }
}
