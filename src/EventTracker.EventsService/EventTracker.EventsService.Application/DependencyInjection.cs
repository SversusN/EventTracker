using EventTracker.EventsService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventTracker.EventsService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();

        return services;
    }
}
