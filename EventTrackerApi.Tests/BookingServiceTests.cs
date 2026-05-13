using EventTrackerApi.DataAccess;
using EventTrackerApi.DataAccess.Repositories;
using EventTrackerApi.Exceptions;
using EventTrackerApi.Models;
using EventTrackerApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventTrackerApi.Tests;

public class BookingServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly EventService _eventService;
    private readonly BookingService _bookingService;

    public BookingServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _eventService = (EventService)_scope.ServiceProvider.GetRequiredService<IEventService>();
        _bookingService = (BookingService)_scope.ServiceProvider.GetRequiredService<IBookingService>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
    }

    private async Task<Event> CreateTestEvent(int totalSeats)
    {
        return await _eventService.CreateEventAsync(
            "Test Event", null, DateTime.Now, DateTime.Now.AddHours(1), totalSeats);
    }

    #region Создание бронирования

    [Fact]
    public async Task CreateBookingAsync_WithValidData_ReturnsCreatedBooking()
    {
        // Arrange
        var @event = await CreateTestEvent(10);

        // Act
        var result = await _bookingService.CreateBookingAsync(@event.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(@event.Id, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task CreateBookingAsync_DecreasesAvailableSeats()
    {
        // Arrange
        var @event = await CreateTestEvent(10);

        // Act
        await _bookingService.CreateBookingAsync(@event.Id);
        var updatedEvent = await _eventService.GetEventByIdAsync(@event.Id);

        // Assert
        Assert.Equal(9, updatedEvent!.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_WithNonExistingEvent_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _bookingService.CreateBookingAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateBookingAsync_WhenNoSeatsAvailable_ThrowsNoAvailableSeatsException()
    {
        // Arrange
        var @event = await CreateTestEvent(1);
        await _bookingService.CreateBookingAsync(@event.Id);

        // Act & Assert
        await Assert.ThrowsAsync<NoAvailableSeatsException>(() => 
            _bookingService.CreateBookingAsync(@event.Id));
    }

    [Fact]
    public async Task CreateBookingAsync_WithConcurrentAccess_DecreasesSeatsCorrectly()
    {
        // Arrange
        const int totalSeats = 10;
        const int concurrentRequests = 5;
        var @event = await CreateTestEvent(totalSeats);

        // Act
        var tasks = new List<Task<Booking>>();
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(_bookingService.CreateBookingAsync(@event.Id));
        }
        await Task.WhenAll(tasks);

        // Assert
        var updatedEvent = await _eventService.GetEventByIdAsync(@event.Id);
        Assert.Equal(totalSeats - concurrentRequests, updatedEvent!.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_WithMoreRequestsThanSeats_ThrowsNoAvailableSeatsException()
    {
        // Arrange
        const int totalSeats = 3;
        const int concurrentRequests = 5;
        var @event = await CreateTestEvent(totalSeats);

        // Act
        var tasks = new List<Task<Booking?>>();
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    return await _bookingService.CreateBookingAsync(@event.Id);
                }
                catch (NoAvailableSeatsException)
                {
                    return null;
                }
            }));
        }
        var results = await Task.WhenAll(tasks);

        // Assert
        var successfulBookings = results.Count(r => r != null);
        var updatedEvent = await _eventService.GetEventByIdAsync(@event.Id);
        Assert.Equal(0, updatedEvent!.AvailableSeats);
        Assert.Equal(totalSeats, successfulBookings);
    }

    #endregion

    #region Получение бронирования по ID

    [Fact]
    public async Task GetBookingByIdAsync_WithExistingId_ReturnsBooking()
    {
        // Arrange
        var @event = await CreateTestEvent(10);
        var createdBooking = await _bookingService.CreateBookingAsync(@event.Id);

        // Act
        var result = await _bookingService.GetBookingByIdAsync(createdBooking.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdBooking.Id, result.Id);
        Assert.Equal(@event.Id, result.EventId);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = await _bookingService.GetBookingByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Проверка статуса бронирования

    [Fact]
    public async Task CreateBookingAsync_DefaultStatusIsPending()
    {
        // Arrange
        var @event = await CreateTestEvent(10);

        // Act
        var booking = await _bookingService.CreateBookingAsync(@event.Id);

        // Assert
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    #endregion
}
