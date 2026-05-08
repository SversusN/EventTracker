using EventTrackerApi.DataAccess;
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
    private readonly IEventService _eventService;
    private readonly IBookingService _bookingService;

    public BookingServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _eventService = _scope.ServiceProvider.GetRequiredService<IEventService>();
        _bookingService = _scope.ServiceProvider.GetRequiredService<IBookingService>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
    }

    private async Task<Guid> CreateTestEventAsync(int totalSeats = 10)
    {
        var created = await _eventService.CreateEventAsync(
            "Test Event",
            null,
            DateTime.Now,
            DateTime.Now.AddHours(1),
            totalSeats);
        return created.Id;
    }

    #region Создание брони - успешные сценарии

    [Fact]
    public async Task CreateBookingAsync_WithExistingEvent_ReturnsBookingWithPendingStatus()
    {
        // Arrange
        var eventId = await CreateTestEventAsync();

        // Act
        var result = await _bookingService.CreateBookingAsync(eventId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.True(result.CreatedAt > DateTime.MinValue);
        Assert.Null(result.ProcessedAt);
    }

    [Fact]
    public async Task CreateBookingAsync_DecreasesAvailableSeatsByOne()
    {
        // Arrange
        var eventId = await CreateTestEventAsync(totalSeats: 5);

        // Act
        var result = await _bookingService.CreateBookingAsync(eventId);

        // Assert
        Assert.NotNull(result);
        var eventInfo = await _eventService.GetEventByIdAsync(eventId);
        Assert.Equal(4, eventInfo!.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_MultipleBookingsUpToLimit_AllSucceedWithUniqueIds()
    {
        // Arrange
        var eventId = await CreateTestEventAsync(totalSeats: 3);

        // Act
        var booking1 = await _bookingService.CreateBookingAsync(eventId);
        var booking2 = await _bookingService.CreateBookingAsync(eventId);
        var booking3 = await _bookingService.CreateBookingAsync(eventId);

        // Assert
        Assert.NotNull(booking1);
        Assert.NotNull(booking2);
        Assert.NotNull(booking3);
        Assert.NotEqual(booking1.Id, booking2.Id);
        Assert.NotEqual(booking2.Id, booking3.Id);
        Assert.NotEqual(booking1.Id, booking3.Id);

        var eventInfo = await _eventService.GetEventByIdAsync(eventId);
        Assert.Equal(0, eventInfo!.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_SetsCreatedAtToCurrentTime()
    {
        // Arrange
        var eventId = await CreateTestEventAsync();
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var result = await _bookingService.CreateBookingAsync(eventId);
        var afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.CreatedAt >= beforeCreation);
        Assert.True(result.CreatedAt <= afterCreation);
    }

    #endregion

    #region Создание брони - неуспешные сценарии

    [Fact]
    public async Task CreateBookingAsync_WithNonExistingEvent_ThrowsNotFoundException()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _bookingService.CreateBookingAsync(eventId));
        Assert.Contains(eventId.ToString(), exception.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenNoSeatsAvailable_ThrowsNoAvailableSeatsException()
    {
        // Arrange
        var eventId = await CreateTestEventAsync(totalSeats: 1);
        await _bookingService.CreateBookingAsync(eventId);

        // Act & Assert
        await Assert.ThrowsAsync<NoAvailableSeatsException>(() => _bookingService.CreateBookingAsync(eventId));
    }

    #endregion

    #region Получение брони по ID - успешные сценарии

    [Fact]
    public async Task GetBookingByIdAsync_WithExistingId_ReturnsBooking()
    {
        // Arrange
        var eventId = await CreateTestEventAsync();
        var createdBooking = await _bookingService.CreateBookingAsync(eventId);
        Assert.NotNull(createdBooking);

        // Act
        var result = await _bookingService.GetBookingByIdAsync(createdBooking.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdBooking.Id, result.Id);
        Assert.Equal(createdBooking.EventId, result.EventId);
        Assert.Equal(createdBooking.Status, result.Status);
    }

    #endregion

    #region Смена статуса брони

    [Fact]
    public void Confirm_SetsStatusToConfirmedAndProcessedAt()
    {
        // Arrange
        var booking = new Booking(Guid.NewGuid());
        var beforeConfirm = DateTime.UtcNow.AddSeconds(-1);

        // Act
        booking.Confirm();
        var afterConfirm = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.True(booking.ProcessedAt >= beforeConfirm);
        Assert.True(booking.ProcessedAt <= afterConfirm);
    }

    [Fact]
    public void Reject_SetsStatusToRejectedAndProcessedAt()
    {
        // Arrange
        var booking = new Booking(Guid.NewGuid());
        var beforeReject = DateTime.UtcNow.AddSeconds(-1);

        // Act
        booking.Reject();
        var afterReject = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.True(booking.ProcessedAt >= beforeReject);
        Assert.True(booking.ProcessedAt <= afterReject);
    }

    [Fact]
    public async Task Reject_ReleaseSeats_RestoresAvailableSeats()
    {
        // Arrange
        var eventId = await CreateTestEventAsync(totalSeats: 5);
        var booking = await _bookingService.CreateBookingAsync(eventId);
        Assert.NotNull(booking);

        var eventInfo = await _eventService.GetEventByIdAsync(eventId);
        Assert.Equal(4, eventInfo!.AvailableSeats);

        // Act
        booking.Reject();
        eventInfo.ReleaseSeats();

        // Assert
        Assert.Equal(5, eventInfo.AvailableSeats);
    }

    [Fact]
    public async Task Reject_ReleaseSeats_AllowsNewBooking()
    {
        // Arrange
        var eventId = await CreateTestEventAsync(totalSeats: 1);
        var booking = await _bookingService.CreateBookingAsync(eventId);
        Assert.NotNull(booking);

        var eventInfo = await _eventService.GetEventByIdAsync(eventId);
        Assert.Equal(0, eventInfo!.AvailableSeats);

        booking.Reject();
        eventInfo.ReleaseSeats();

        // Act
        var newBooking = await _bookingService.CreateBookingAsync(eventId);

        // Assert
        Assert.NotNull(newBooking);
        eventInfo = await _eventService.GetEventByIdAsync(eventId);
        Assert.Equal(0, eventInfo!.AvailableSeats);
    }

    #endregion

    #region Получение брони по ID - неуспешные сценарии

    [Fact]
    public async Task GetBookingByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _bookingService.GetBookingByIdAsync(nonExistingId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Обработка нескольких событий

    [Fact]
    public async Task CreateBookingAsync_ForDifferentEvents_CreatesCorrectBookings()
    {
        // Arrange
        var eventId1 = await _eventService.CreateEventAsync("Event 1", null, DateTime.Now, DateTime.Now.AddHours(1), 10);
        var eventId2 = await _eventService.CreateEventAsync("Event 2", null, DateTime.Now.AddHours(2), DateTime.Now.AddHours(3), 10);

        // Act
        var booking1 = await _bookingService.CreateBookingAsync(eventId1.Id);
        var booking2 = await _bookingService.CreateBookingAsync(eventId2.Id);

        // Assert
        Assert.NotNull(booking1);
        Assert.NotNull(booking2);
        Assert.Equal(eventId1.Id, booking1.EventId);
        Assert.Equal(eventId2.Id, booking2.EventId);
    }

    #endregion

    #region Конкурентность

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_PreventOverbooking()
    {
        // Arrange
        const int totalSeats = 5;
        const int concurrentRequests = 20;
        var eventId = await CreateTestEventAsync(totalSeats: totalSeats);

        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                try
                {
                    await bookingService.CreateBookingAsync(eventId);
                    return true;
                }
                catch (NoAvailableSeatsException)
                {
                    return false;
                }
            }));

        var results = await Task.WhenAll(tasks);

        // Assert
        var successCount = results.Count(r => r);
        Assert.Equal(totalSeats, successCount);
    }

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_AllSuccessfulHaveUniqueIds()
    {
        // Arrange
        const int totalSeats = 10;
        const int concurrentRequests = 10;
        var eventId = await CreateTestEventAsync(totalSeats: totalSeats);
        var bookingIds = new System.Collections.Concurrent.ConcurrentBag<Guid>();

        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                var booking = await bookingService.CreateBookingAsync(eventId);
                bookingIds.Add(booking.Id);
            }));

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(totalSeats, bookingIds.Distinct().Count());
    }

    #endregion
}
