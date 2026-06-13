using EventTrackerApi.Application.Options;
using EventTrackerApi.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventTrackerApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddOptions<BookingOptions>();

        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IUserService, UserService>();
        services.AddHostedService<BookingProcessingService>();

        return services;
    }
}
