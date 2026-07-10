using Confluent.Kafka;
using EventTracker.EventsService.Application.Ports;
using EventTracker.EventsService.Infrastructure.DataAccess;
using EventTracker.EventsService.Infrastructure.DataAccess.Repositories;
using EventTracker.EventsService.Infrastructure.Messaging.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventTracker.EventsService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IEventRepository, EventRepository>();

        services.AddSingleton<IAdminClient>(sp =>
        {
            var bootstrapServers = configuration["Kafka:BootstrapServers"]
                ?? throw new InvalidOperationException("Kafka:BootstrapServers is not configured.");

            var config = new AdminClientConfig
            {
                BootstrapServers = bootstrapServers
            };

            return new AdminClientBuilder(config).Build();
        });

        services.AddHostedService<KafkaTopicInitializer>();
        services.AddHostedService<BookingConfirmedConsumer>();

        return services;
    }
}
