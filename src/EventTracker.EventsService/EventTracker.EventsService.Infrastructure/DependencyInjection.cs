using Confluent.Kafka;
using EventTracker.EventsService.Application.Options;
using EventTracker.EventsService.Application.Ports;
using EventTracker.EventsService.Infrastructure.Caching;
using EventTracker.EventsService.Infrastructure.DataAccess;
using EventTracker.EventsService.Infrastructure.DataAccess.Repositories;
using EventTracker.EventsService.Infrastructure.Messaging.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace EventTracker.EventsService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IEventRepository, EventRepository>();

        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        var redisConnectionString = configuration["Redis:ConnectionString"]
            ?? throw new InvalidOperationException("Redis:ConnectionString is not configured.");

        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddSingleton<ICacheService, RedisCacheService>();

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
