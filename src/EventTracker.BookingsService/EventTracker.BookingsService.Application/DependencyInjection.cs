using EventTracker.BookingsService.Application.Options;
using EventTracker.BookingsService.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventTracker.BookingsService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BookingOptions>(configuration.GetSection(BookingOptions.SectionName));

        services.AddScoped<IBookingService, BookingService>();
        services.AddHostedService<BookingProcessingService>();

        return services;
    }
}
