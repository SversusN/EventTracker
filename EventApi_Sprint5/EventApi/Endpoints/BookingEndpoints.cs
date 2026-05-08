using EventApi.Dto;
using EventApi.Exceptions;
using EventApi.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace EventApi.Endpoints;

internal static class BookingEndpoints
{
    internal static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/events/{id:guid}/book", async (
            Guid id,
            IBookingService bookingService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var booking = await bookingService.CreateBookingAsync(id, cancellationToken);

            var location = $"/bookings/{booking.Id}";
            httpContext.Response.Headers.Location = location;

            return Results.Accepted(location, booking);
        })
        .WithName("CreateBooking")
        .Produces<BookingInfo>(StatusCodes.Status202Accepted)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        app.MapGet("/bookings/{id:guid}", async (
            Guid id,
            IBookingService bookingService,
            CancellationToken cancellationToken) =>
        {
            var booking = await bookingService.GetBookingByIdAsync(id, cancellationToken);
            return Results.Ok(booking);
        })
        .WithName("GetBookingById")
        .Produces<BookingInfo>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return app;
    }
}
