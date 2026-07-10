using EventTracker.BookingsService.Application.Ports;
using EventTracker.BookingsService.Infrastructure.DataAccess;
using EventTracker.BookingsService.Infrastructure.DataAccess.Repositories;
using EventTracker.BookingsService.Infrastructure.Messaging.Kafka;
using EventTracker.Contracts.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventTracker.BookingsService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IBookingRepository, BookingRepository>();

        services.AddSingleton<IMessagePublisher<BookingConfirmedEvent>, KafkaBookingConfirmedPublisher>();

        return services;
    }
}
