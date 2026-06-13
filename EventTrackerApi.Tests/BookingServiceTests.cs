using EventTrackerApi.Domain.Models;
using EventTrackerApi.Domain.Exceptions;
using EventTrackerApi.Application.Ports;
using EventTrackerApi.Application.Services;
using EventTrackerApi.Infrastructure.DataAccess;
using EventTrackerApi.Infrastructure.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventTrackerApi.Tests;

public class BookingServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly EventService _eventService;
    private readonly BookingService _bookingService;
    private readonly IUserRepository _userRepository;

    public BookingServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _eventService = (EventService)_scope.ServiceProvider.GetRequiredService<IEventService>();
        _bookingService = (BookingService)_scope.ServiceProvider.GetRequiredService<IBookingService>();
        _userRepository = _scope.ServiceProvider.GetRequiredService<IUserRepository>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
    }

    private async Task<Event> CreateTestEvent(int totalSeats, DateTime? startAt = null)
    {
        return await _eventService.CreateEventAsync(
            "Test Event", null, startAt ?? DateTime.Now.AddHours(1), DateTime.Now.AddHours(2), totalSeats);
    }

    private async Task<Guid> CreateTestUser(string login = "testuser")
    {
        var user = new User(login, "passwordHash123");
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();
        return user.Id;
    }

    #region Создание бронирования

    [Fact]
    public async Task CreateBookingAsync_WithValidData_ReturnsCreatedBooking()
    {
        // Arrange
        var userId = await CreateTestUser();
        var @event = await CreateTestEvent(10);

        // Act
        var result = await _bookingService.CreateBookingAsync(@event.Id, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(@event.Id, result.EventId);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task CreateBookingAsync_DecreasesAvailableSeats()
    {
        // Arrange
        var userId = await CreateTestUser();
        var @event = await CreateTestEvent(10);

        // Act
        await _bookingService.CreateBookingAsync(@event.Id, userId);
        var updatedEvent = await _eventService.GetEventByIdAsync(@event.Id);

        // Assert
        Assert.Equal(9, updatedEvent!.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_WithNonExistingEvent_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _bookingService.CreateBookingAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateBookingAsync_WhenNoSeatsAvailable_ThrowsNoAvailableSeatsException()
    {
        // Arrange
        var userId = await CreateTestUser();
        var @event = await CreateTestEvent(1);
        await _bookingService.CreateBookingAsync(@event.Id, userId);

        var otherUserId = await CreateTestUser("otheruser");

        // Act & Assert
        await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            _bookingService.CreateBookingAsync(@event.Id, otherUserId));
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
            var userId = await CreateTestUser($"user{i}");
            tasks.Add(_bookingService.CreateBookingAsync(@event.Id, userId));
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
            var userId = await CreateTestUser($"user{i}");
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    return await _bookingService.CreateBookingAsync(@event.Id, userId);
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

    [Fact]
    public async Task CreateBookingAsync_WhenEventAlreadyStarted_ThrowsEventAlreadyStartedException()
    {
        // Arrange
        var userId = await CreateTestUser();
        var @event = await _eventService.CreateEventAsync(
            "Past Event", null, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1), 10);

        // Act & Assert
        await Assert.ThrowsAsync<EventAlreadyStartedException>(() =>
            _bookingService.CreateBookingAsync(@event.Id, userId));
    }

    [Fact]
    public async Task CreateBookingAsync_WhenActiveBookingLimitExceeded_ThrowsBookingLimitExceededException()
    {
        // Arrange
        var userId = await CreateTestUser();
        for (int i = 0; i < 10; i++)
        {
            var @event = await CreateTestEvent(1);
            await _bookingService.CreateBookingAsync(@event.Id, userId);
        }

        var anotherEvent = await CreateTestEvent(10);

        // Act & Assert
        await Assert.ThrowsAsync<BookingLimitExceededException>(() =>
            _bookingService.CreateBookingAsync(anotherEvent.Id, userId));
    }

    [Fact]
    public async Task CreateBookingAsync_ActiveBookingLimitPerUser_DoesNotAffectOtherUsers()
    {
        // Arrange
        var userId = await CreateTestUser("user1");
        var otherUserId = await CreateTestUser("user2");

        for (int i = 0; i < 10; i++)
        {
            var @event = await CreateTestEvent(1);
            await _bookingService.CreateBookingAsync(@event.Id, userId);
        }

        var sharedEvent = await CreateTestEvent(10);

        // Act
        var booking = await _bookingService.CreateBookingAsync(sharedEvent.Id, otherUserId);

        // Assert
        Assert.NotNull(booking);
        Assert.Equal(otherUserId, booking.UserId);
    }

    #endregion

    #region Получение бронирования по ID

    [Fact]
    public async Task GetBookingByIdAsync_WithExistingId_ReturnsBooking()
    {
        // Arrange
        var userId = await CreateTestUser();
        var @event = await CreateTestEvent(10);
        var createdBooking = await _bookingService.CreateBookingAsync(@event.Id, userId);

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
        var userId = await CreateTestUser();
        var @event = await CreateTestEvent(10);

        // Act
        var booking = await _bookingService.CreateBookingAsync(@event.Id, userId);

        // Assert
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    #endregion
}
