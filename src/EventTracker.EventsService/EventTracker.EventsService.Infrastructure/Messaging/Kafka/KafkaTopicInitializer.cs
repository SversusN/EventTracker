using Confluent.Kafka;
using Confluent.Kafka.Admin;
using EventTracker.Contracts.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventTracker.EventsService.Infrastructure.Messaging.Kafka;

/// <summary>
/// Hosted service для создания Kafka топиков при старте приложения
/// </summary>
public class KafkaTopicInitializer : BackgroundService
{
    private readonly IAdminClient _adminClient;
    private readonly ILogger<KafkaTopicInitializer> _logger;

    public KafkaTopicInitializer(IAdminClient adminClient, ILogger<KafkaTopicInitializer> logger)
    {
        _adminClient = adminClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _adminClient.CreateTopicsAsync(new[]
            {
                new TopicSpecification
                {
                    Name = TopicNames.BookingConfirmed,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                }
            });

            _logger.LogInformation("Kafka topic {Topic} created", TopicNames.BookingConfirmed);
        }
        catch (CreateTopicsException ex) when (ex.Results.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            _logger.LogInformation("Kafka topic {Topic} already exists", TopicNames.BookingConfirmed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Kafka topic {Topic}", TopicNames.BookingConfirmed);
        }
    }
}
