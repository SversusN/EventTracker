using System.Security.Claims;
using EventTrackerApi.Presentation.Infrastructure;
using EventTrackerApi.Application.DTOs;
using EventTrackerApi.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventTrackerApi.Presentation.Controllers;

/// <summary>
/// Контроллер для управления бронированиями
/// </summary>
[ApiController]
[Route("bookings")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
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

        var response = new BookingResponseDto(
            booking.Id,
            booking.EventId,
            booking.UserId,
            booking.Status,
            booking.CreatedAt,
            booking.ProcessedAt
        );

        return Ok(response);
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
        var userId = GetCurrentUserId();
        await bookingService.CancelBookingAsync(id, userId);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException("User identifier is missing or invalid.");
        }
        return userId;
    }
}
