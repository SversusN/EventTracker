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

    #region Создание брони - успешные сценарии

    [Fact]
    public async Task CreateBookingAsync_WithExistingEvent_ReturnsBookingWithPendingStatus()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = new Event("Test Event", null, DateTime.Now, DateTime.Now.AddHours(1));
        
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
    public async Task CreateBookingAsync_MultipleBookingsForSameEvent_ReturnsUniqueIds()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = new Event("Test Event", null, DateTime.Now, DateTime.Now.AddHours(1));
        
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
    }

    [Fact]
    public async Task CreateBookingAsync_SetsCreatedAtToCurrentTime()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = new Event("Test Event", null, DateTime.Now, DateTime.Now.AddHours(1));
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
    public async Task CreateBookingAsync_WithNonExistingEvent_ReturnsNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        
        _eventServiceMock
            .Setup(s => s.GetEventById(eventId))
            .Returns((Event?)null);

        // Act
        var result = await _bookingService.CreateBookingAsync(eventId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateBookingAsync_ForDeletedEvent_ReturnsNull()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        
        _eventServiceMock
            .Setup(s => s.GetEventById(eventId))
            .Returns((Event?)null);

        // Act
        var result = await _bookingService.CreateBookingAsync(eventId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Получение брони по ID - успешные сценарии

    [Fact]
    public async Task GetBookingByIdAsync_WithExistingId_ReturnsBooking()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = new Event("Test Event", null, DateTime.Now, DateTime.Now.AddHours(1));
        
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

    [Fact]
    public async Task GetBookingByIdAsync_ReflectsStatusChange_AfterConfirm()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = new Event("Test Event", null, DateTime.Now, DateTime.Now.AddHours(1));
        
        _eventServiceMock
            .Setup(s => s.GetEventById(eventId))
            .Returns(eventItem);

        var booking = await _bookingService.CreateBookingAsync(eventId);
        Assert.NotNull(booking);
        Assert.Equal(BookingStatus.Pending, booking.Status);

        // Изменяем статус через метод Confirm
        booking.Confirm();
        await _bookingService.UpdateBookingAsync(booking);

        // Act
        var result = await _bookingService.GetBookingByIdAsync(booking.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Confirmed, result.Status);
        Assert.NotNull(result.ProcessedAt);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ReflectsStatusChange_AfterReject()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventItem = new Event("Test Event", null, DateTime.Now, DateTime.Now.AddHours(1));
        
        _eventServiceMock
            .Setup(s => s.GetEventById(eventId))
            .Returns(eventItem);

        var booking = await _bookingService.CreateBookingAsync(eventId);
        Assert.NotNull(booking);
        Assert.Equal(BookingStatus.Pending, booking.Status);

        // Изменяем статус через метод Reject
        booking.Reject();
        await _bookingService.UpdateBookingAsync(booking);

        // Act
        var result = await _bookingService.GetBookingByIdAsync(booking.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Rejected, result.Status);
        Assert.NotNull(result.ProcessedAt);
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
        var eventItem = new Event("Test Event", null, DateTime.Now, DateTime.Now.AddHours(1));
        
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
        var eventItem = new Event("Test Event", null, DateTime.Now, DateTime.Now.AddHours(1));
        
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
        var eventItem = new Event("Test Event", null, DateTime.Now, DateTime.Now.AddHours(1));
        
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
        var eventItem1 = new Event("Event 1", null, DateTime.Now, DateTime.Now.AddHours(1));
        var eventItem2 = new Event("Event 2", null, DateTime.Now.AddHours(2), DateTime.Now.AddHours(3));
        
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
}
