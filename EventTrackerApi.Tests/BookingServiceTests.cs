using EventTrackerApi.Exceptions;
using EventTrackerApi.Models;
using EventTrackerApi.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventTrackerApi.Tests;

public class BookingServiceTests
{
    private readonly Mock<IEventService> _eventServiceMock;
    private readonly Mock<ILogger<BookingService>> _loggerMock;
    private readonly BookingService _bookingService;

    public BookingServiceTests()
    {
        _eventServiceMock = new Mock<IEventService>();
        _loggerMock = new Mock<ILogger<BookingService>>();
        _bookingService = new BookingService(_eventServiceMock.Object, _loggerMock.Object);
    }

    private static Event CreateTestEvent(int totalSeats = 10)
    {
        return new Event("Test Event", null, DateTime.Now, DateTime.Now.AddHours(1), totalSeats);
    }

    #region Создание брони - успешные сценарии

    [Fact]
    public async Task CreateBookingAsync_WithExistingEvent_ReturnsBookingWithPendingStatus()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent();
        eventItem = new Event(eventItem.Id, eventItem.Title, eventItem.Description, eventItem.StartAt, eventItem.EndAt, eventItem.TotalSeats, eventItem.AvailableSeats);
        
        _eventServiceMock
            .Setup(s => s.GetEventById(eventId))
            .Returns(eventItem);

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
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent(totalSeats: 5);
        
        _eventServiceMock
            .Setup(s => s.GetEventById(eventId))
            .Returns(eventItem);

        // Act
        var result = await _bookingService.CreateBookingAsync(eventId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, eventItem.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_MultipleBookingsUpToLimit_AllSucceedWithUniqueIds()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent(totalSeats: 3);
        
        _eventServiceMock
            .Setup(s => s.GetEventById(eventId))
            .Returns(eventItem);

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
        Assert.Equal(0, eventItem.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_SetsCreatedAtToCurrentTime()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent();
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);
        
        _eventServiceMock
            .Setup(s => s.GetEventById(eventId))
            .Returns(eventItem);

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
        
        _eventServiceMock
            .Setup(s => s.GetEventById(eventId))
            .Returns((Event?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _bookingService.CreateBookingAsync(eventId));
    }

    [Fact]
    public async Task CreateBookingAsync_WhenNoSeatsAvailable_ThrowsNoAvailableSeatsException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent(totalSeats: 1);
        
        _eventServiceMock
            .Setup(s => s.GetEventById(eventId))
            .Returns(eventItem);

        await _bookingService.CreateBookingAsync(eventId);

        // Act & Assert
        await Assert.ThrowsAsync<NoAvailableSeatsException>(() => _bookingService.CreateBookingAsync(eventId));
    }

    [Fact]
    public async Task CreateBookingAsync_ForDeletedEvent_ThrowsNotFoundException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        
        _eventServiceMock
            .Setup(s => s.GetEventById(eventId))
            .Returns((Event?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _bookingService.CreateBookingAsync(eventId));
    }

    #endregion

    #region Получение брони по ID - успешные сценарии

    [Fact]
    public async Task GetBookingByIdAsync_WithExistingId_ReturnsBooking()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent();
        
        _eventServiceMock
            .Setup(s => s.GetEventById(eventId))
            .Returns(eventItem);

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
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent(totalSeats: 5);
        
        _eventServiceMock
            .Setup(s => s.GetEventById(eventId))
            .Returns(eventItem);

        var booking = await _bookingService.CreateBookingAsync(eventId);
        Assert.NotNull(booking);
        Assert.Equal(4, eventItem.AvailableSeats);

        // Act
        booking.Reject();
        eventItem.ReleaseSeats();

        // Assert
        Assert.Equal(5, eventItem.AvailableSeats);
    }

    [Fact]
    public async Task Reject_ReleaseSeats_AllowsNewBooking()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent(totalSeats: 1);
        
        _eventServiceMock
            .Setup(s => s.GetEventById(eventId))
            .Returns(eventItem);

        var booking = await _bookingService.CreateBookingAsync(eventId);
        Assert.NotNull(booking);
        Assert.Equal(0, eventItem.AvailableSeats);

        booking.Reject();
        eventItem.ReleaseSeats();

        // Act
        var newBooking = await _bookingService.CreateBookingAsync(eventId);

        // Assert
        Assert.NotNull(newBooking);
        Assert.Equal(0, eventItem.AvailableSeats);
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

    #region Получение броней по статусу

    [Fact]
    public async Task GetBookingsByStatusAsync_WithPendingStatus_ReturnsOnlyPendingBookings()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent();
        
        _eventServiceMock
            .Setup(s => s.GetEventById(eventId))
            .Returns(eventItem);

        var booking1 = await _bookingService.CreateBookingAsync(eventId);
        var booking2 = await _bookingService.CreateBookingAsync(eventId);
        var booking3 = await _bookingService.CreateBookingAsync(eventId);

        Assert.NotNull(booking1);
        Assert.NotNull(booking2);
        Assert.NotNull(booking3);

        // Подтверждаем одну бронь
        booking1.Confirm();
        await _bookingService.UpdateBookingAsync(booking1);

        // Act
        var pendingBookings = await _bookingService.GetBookingsByStatusAsync(BookingStatus.Pending);

        // Assert
        Assert.Equal(2, pendingBookings.Count());
        Assert.All(pendingBookings, b => Assert.Equal(BookingStatus.Pending, b.Status));
        Assert.DoesNotContain(pendingBookings, b => b.Id == booking1.Id);
    }

    [Fact]
    public async Task GetBookingsByStatusAsync_WithNoBookings_ReturnsEmptyList()
    {
        // Act
        var result = await _bookingService.GetBookingsByStatusAsync(BookingStatus.Pending);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBookingsByStatusAsync_WithConfirmedStatus_ReturnsOnlyConfirmedBookings()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent();
        
        _eventServiceMock
            .Setup(s => s.GetEventById(eventId))
            .Returns(eventItem);

        var booking1 = await _bookingService.CreateBookingAsync(eventId);
        var booking2 = await _bookingService.CreateBookingAsync(eventId);

        Assert.NotNull(booking1);
        Assert.NotNull(booking2);

        // Подтверждаем обе брони
        booking1.Confirm();
        booking2.Confirm();
        await _bookingService.UpdateBookingAsync(booking1);
        await _bookingService.UpdateBookingAsync(booking2);

        // Act
        var confirmedBookings = await _bookingService.GetBookingsByStatusAsync(BookingStatus.Confirmed);

        // Assert
        Assert.Equal(2, confirmedBookings.Count());
        Assert.All(confirmedBookings, b => Assert.Equal(BookingStatus.Confirmed, b.Status));
    }

    #endregion

    #region Обновление брони

    [Fact]
    public async Task UpdateBookingAsync_WithExistingBooking_ReturnsTrue()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent();
        
        _eventServiceMock
            .Setup(s => s.GetEventById(eventId))
            .Returns(eventItem);

        var booking = await _bookingService.CreateBookingAsync(eventId);
        Assert.NotNull(booking);

        // Act
        booking.Confirm();
        var result = await _bookingService.UpdateBookingAsync(booking);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UpdateBookingAsync_WithNonExistingBooking_ReturnsFalse()
    {
        // Arrange
        var nonExistingBooking = new Booking(
            Guid.NewGuid(),
            Guid.NewGuid(),
            BookingStatus.Pending,
            DateTime.UtcNow,
            null
        );

        // Act
        var result = await _bookingService.UpdateBookingAsync(nonExistingBooking);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Обработка нескольких событий

    [Fact]
    public async Task CreateBookingAsync_ForDifferentEvents_CreatesCorrectBookings()
    {
        // Arrange
        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();
        var eventItem1 = new Event("Event 1", null, DateTime.Now, DateTime.Now.AddHours(1), 10);
        var eventItem2 = new Event("Event 2", null, DateTime.Now.AddHours(2), DateTime.Now.AddHours(3), 10);
        
        _eventServiceMock
            .Setup(s => s.GetEventById(eventId1))
            .Returns(eventItem1);
        _eventServiceMock
            .Setup(s => s.GetEventById(eventId2))
            .Returns(eventItem2);

        // Act
        var booking1 = await _bookingService.CreateBookingAsync(eventId1);
        var booking2 = await _bookingService.CreateBookingAsync(eventId2);

        // Assert
        Assert.NotNull(booking1);
        Assert.NotNull(booking2);
        Assert.Equal(eventId1, booking1.EventId);
        Assert.Equal(eventId2, booking2.EventId);
    }

    #endregion

    #region Конкурентность

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_PreventOverbooking()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent(totalSeats: 5);
        
        _eventServiceMock
            .Setup(s => s.GetEventById(eventId))
            .Returns(eventItem);

        var tasks = new List<Task>();
        var successfulBookings = new List<Booking>();
        var exceptions = new List<Exception>();
        var lockObj = new object();

        // Act
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var booking = await _bookingService.CreateBookingAsync(eventId);
                    lock (lockObj)
                    {
                        successfulBookings.Add(booking);
                    }
                }
                catch (Exception ex)
                {
                    lock (lockObj)
                    {
                        exceptions.Add(ex);
                    }
                }
            }, TestContext.Current.CancellationToken));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(5, successfulBookings.Count);
        Assert.Equal(15, exceptions.Count);
        Assert.All(exceptions, ex => Assert.IsType<NoAvailableSeatsException>(ex));
        Assert.Equal(0, eventItem.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_AllSuccessfulHaveUniqueIds()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = CreateTestEvent(totalSeats: 10);
        
        _eventServiceMock
            .Setup(s => s.GetEventById(eventId))
            .Returns(eventItem);

        var tasks = new List<Task>();
        var successfulBookings = new List<Booking>();
        var lockObj = new object();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var booking = await _bookingService.CreateBookingAsync(eventId);
                lock (lockObj)
                {
                    successfulBookings.Add(booking);
                }
            }, TestContext.Current.CancellationToken));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, successfulBookings.Count);
        var uniqueIds = successfulBookings.Select(b => b.Id).ToHashSet();
        Assert.Equal(10, uniqueIds.Count);
        Assert.Equal(0, eventItem.AvailableSeats);
    }

    #endregion
}
