using EventTrackerApi.Infrastructure;
using EventTrackerApi.Models.Dto;
using EventTrackerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventTrackerApi.Controllers;

/// <summary>
/// Контроллер для управления бронированиями
/// </summary>
[ApiController]
[Route("bookings")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{

    /// <summary>
    /// Получить бронирование по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор брони (GUID)</param>
    /// <returns>Бронирование с указанным идентификатором</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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
            booking.Status,
            booking.CreatedAt,
            booking.ProcessedAt
        );

        return Ok(response);
    }
}
