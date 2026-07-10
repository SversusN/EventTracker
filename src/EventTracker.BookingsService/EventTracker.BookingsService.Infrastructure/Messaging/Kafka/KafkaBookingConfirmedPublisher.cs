using System.Text.Json;
using Confluent.Kafka;
using EventTracker.BookingsService.Application.Ports;
using EventTracker.Contracts.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventTracker.BookingsService.Infrastructure.Messaging.Kafka;

public class KafkaBookingConfirmedPublisher : IMessagePublisher<BookingConfirmedEvent>, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaBookingConfirmedPublisher> _logger;

    public KafkaBookingConfirmedPublisher(IConfiguration configuration, ILogger<KafkaBookingConfirmedPublisher> logger)
    {
        var bootstrapServers = configuration["Kafka:BootstrapServers"]
            ?? throw new InvalidOperationException("Kafka BootstrapServers is not configured.");

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        _logger = logger;
    }

    public async Task PublishAsync(BookingConfirmedEvent message, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message);

        var kafkaMessage = new Message<string, string>
        {
            Key = message.EventId.ToString(),
            Value = json
        };

        var deliveryResult = await _producer.ProduceAsync(TopicNames.BookingConfirmed, kafkaMessage, cancellationToken);

        _logger.LogInformation(
            "Published BookingConfirmed event to {Topic} partition {Partition} offset {Offset}",
            deliveryResult.Topic,
            deliveryResult.Partition,
            deliveryResult.Offset);
    }

    public void Dispose()
    {
        _producer.Dispose();
        GC.SuppressFinalize(this);
    }
}
