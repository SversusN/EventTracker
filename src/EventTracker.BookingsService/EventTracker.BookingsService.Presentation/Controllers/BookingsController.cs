using System.Security.Claims;
using EventTracker.BookingsService.Application.DTOs;
using EventTracker.BookingsService.Application.Services;
using EventTracker.BookingsService.Presentation.Infrastructure;
using EventTracker.BookingsService.Presentation.Infrastructure.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventTracker.BookingsService.Presentation.Controllers;

/// <summary>
/// Контроллер для управления бронированиями
/// </summary>
[ApiController]
[Route("bookings")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    /// <summary>
    /// Создать бронирование (требуется аутентификация)
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
    {
        var userId = this.GetCurrentUserId();
        var booking = await bookingService.CreateBookingAsync(dto.EventId, userId);

        var response = MapToResponse(booking);
        return CreatedAtAction(nameof(GetBookingById), new { id = booking.Id }, response);
    }

    /// <summary>
    /// Получить бронирование по идентификатору (требуется аутентификация)
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetBookingById(Guid id)
    {
        var booking = await bookingService.GetBookingByIdAsync(id);
        if (booking is null)
        {
            return NotFound(ProblemDetailsHelper.NotFound("Бронирование", id));
        }

        return Ok(MapToResponse(booking));
    }

    /// <summary>
    /// Отменить бронирование (требуется аутентификация)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CancelBooking(Guid id)
    {
        var userId = this.GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");
        await bookingService.CancelBookingAsync(id, userId, isAdmin);
        return NoContent();
    }

    private static BookingResponseDto MapToResponse(Domain.Models.Booking booking) =>
        new(
            booking.Id,
            booking.EventId,
            booking.UserId,
            booking.Status,
            booking.CreatedAt,
            booking.ProcessedAt);
}
