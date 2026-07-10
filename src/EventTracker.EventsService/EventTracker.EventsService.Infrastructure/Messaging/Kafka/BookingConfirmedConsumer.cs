using System.Text.Json;
using Confluent.Kafka;
using EventTracker.Contracts.Kafka;
using EventTracker.EventsService.Application.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventTracker.EventsService.Infrastructure.Messaging.Kafka;

/// <summary>
/// Kafka consumer, обрабатывающий события подтверждения бронирования
/// и уменьшающий количество доступных мест
/// </summary>
public class BookingConfirmedConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BookingConfirmedConsumer> _logger;

    public BookingConfirmedConsumer(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<BookingConfirmedConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConsumeAsync(stoppingToken);
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"]
            ?? throw new InvalidOperationException("Kafka:BootstrapServers is not configured.");
        var consumerGroup = _configuration["Kafka:ConsumerGroup"]
            ?? "events-service";

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = consumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(TopicNames.BookingConfirmed);

        _logger.LogInformation("Kafka consumer subscribed to topic {Topic}", TopicNames.BookingConfirmed);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    if (consumeResult?.Message?.Value is null)
                    {
                        continue;
                    }

                    var bookingEvent = JsonSerializer.Deserialize<BookingConfirmedEvent>(
                        consumeResult.Message.Value,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (bookingEvent is null)
                    {
                        _logger.LogWarning("Unable to deserialize Kafka message: {Message}", consumeResult.Message.Value);
                        continue;
                    }

                    await ProcessBookingConfirmedAsync(bookingEvent, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing booking confirmed message");
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ProcessBookingConfirmedAsync(BookingConfirmedEvent bookingEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing BookingConfirmed: BookingId={BookingId}, EventId={EventId}, Seats={Seats}",
            bookingEvent.BookingId,
            bookingEvent.EventId,
            bookingEvent.Seats);

        if (bookingEvent.Seats <= 0)
        {
            _logger.LogWarning("Invalid Seats value {Seats} for booking {BookingId}, skipping", bookingEvent.Seats, bookingEvent.BookingId);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

        var ev = await repository.GetByIdAsync(bookingEvent.EventId);
        if (ev is null)
        {
            _logger.LogWarning("Event {EventId} not found for booking {BookingId}, skipping", bookingEvent.EventId, bookingEvent.BookingId);
            return;
        }

        if (!ev.TryReserveSeats(bookingEvent.Seats))
        {
            _logger.LogWarning(
                "Not enough available seats for event {EventId} (requested {Seats}, available {Available}), skipping",
                bookingEvent.EventId,
                bookingEvent.Seats,
                ev.AvailableSeats);
            return;
        }

        await repository.SaveChangesAsync();

        _logger.LogInformation(
            "Decreased available seats for event {EventId} by {Seats}. Remaining: {Available}",
            bookingEvent.EventId,
            bookingEvent.Seats,
            ev.AvailableSeats);
    }
}
