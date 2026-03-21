using EventTrackerApi.Models;
using EventTrackerApi.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventTrackerApi.Tests;

public class EventServiceTests
{
    private readonly EventService _eventService;
    private readonly Mock<ILogger<EventService>> _loggerMock;

    public EventServiceTests()
    {
        _loggerMock = new Mock<ILogger<EventService>>();
        _eventService = new EventService(_loggerMock.Object);
    }

    #region Создание события

    [Fact]
    public void CreateEvent_WithValidData_ReturnsCreatedEvent()
    {
        // Arrange
        var title = "Test Event";
        var description = "Test Description";
        var startAt = DateTime.Now;
        var endAt = DateTime.Now.AddHours(1);

        // Act
        var result = _eventService.CreateEvent(title, description, startAt, endAt);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(title, result.Title);
        Assert.Equal(description, result.Description);
        Assert.Equal(startAt, result.StartAt);
        Assert.Equal(endAt, result.EndAt);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Theory]
    [InlineData("", "Description", "2026-01-01", "2026-01-02")]
    [InlineData("Title", "Description", "2026-01-05", "2026-01-01")]
    public void CreateEvent_WithInvalidData_ThrowsArgumentException(string title, string? description, string startAtStr, string endAtStr)
    {
        // Arrange
        var startAt = DateTime.Parse(startAtStr);
        var endAt = DateTime.Parse(endAtStr);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _eventService.CreateEvent(title, description, startAt, endAt));
    }

    #endregion

    #region Получение всех событий

    [Fact]
    public void GetEvents_WithNoEvents_ReturnsEmptyPaginatedResult()
    {
        // Act
        var result = _eventService.GetEvents();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public void GetEvents_WithMultipleEvents_ReturnsAllEvents()
    {
        // Arrange
        _eventService.CreateEvent("Event 1", null, DateTime.Now, DateTime.Now.AddHours(1));
        _eventService.CreateEvent("Event 2", null, DateTime.Now, DateTime.Now.AddHours(1));
        _eventService.CreateEvent("Event 3", null, DateTime.Now, DateTime.Now.AddHours(1));

        // Act
        var result = _eventService.GetEvents();

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count());
    }

    #endregion

    #region Получение события по ID

    [Fact]
    public void GetEventById_WithExistingId_ReturnsEvent()
    {
        // Arrange
        var createdEvent = _eventService.CreateEvent("Test", null, DateTime.Now, DateTime.Now.AddHours(1));

        // Act
        var result = _eventService.GetEventById(createdEvent.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdEvent.Id, result.Id);
        Assert.Equal(createdEvent.Title, result.Title);
    }

    [Fact]
    public void GetEventById_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = _eventService.GetEventById(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Обновление события

    [Fact]
    public void UpdateEvent_WithExistingId_ReturnsUpdatedEvent()
    {
        // Arrange
        var createdEvent = _eventService.CreateEvent("Original", null, DateTime.Now, DateTime.Now.AddHours(1));
        var newTitle = "Updated Title";
        var newDescription = "Updated Description";
        var newStartAt = DateTime.Now.AddDays(1);
        var newEndAt = DateTime.Now.AddDays(1).AddHours(1);

        // Act
        var result = _eventService.UpdateEvent(createdEvent.Id, newTitle, newDescription, newStartAt, newEndAt);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newTitle, result.Title);
        Assert.Equal(newDescription, result.Description);
        Assert.Equal(newStartAt, result.StartAt);
        Assert.Equal(newEndAt, result.EndAt);
    }

    [Fact]
    public void UpdateEvent_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = _eventService.UpdateEvent(Guid.NewGuid(), "Title", null, DateTime.Now, DateTime.Now.AddHours(1));

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("", "Description", "2026-01-01", "2026-01-02")]
    [InlineData("Title", "Description", "2026-01-05", "2026-01-01")]
    public void UpdateEvent_WithInvalidData_ThrowsArgumentException(string title, string? description, string startAtStr, string endAtStr)
    {
        // Arrange
        var createdEvent = _eventService.CreateEvent("Original", null, DateTime.Now, DateTime.Now.AddHours(1));
        var startAt = DateTime.Parse(startAtStr);
        var endAt = DateTime.Parse(endAtStr);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _eventService.UpdateEvent(createdEvent.Id, title, description, startAt, endAt));
    }

    #endregion

    #region Удаление события

    [Fact]
    public void DeleteEvent_WithExistingId_ReturnsTrue()
    {
        // Arrange
        var createdEvent = _eventService.CreateEvent("Test", null, DateTime.Now, DateTime.Now.AddHours(1));

        // Act
        var result = _eventService.DeleteEvent(createdEvent.Id);

        // Assert
        Assert.True(result);
        Assert.Null(_eventService.GetEventById(createdEvent.Id));
    }

    [Fact]
    public void DeleteEvent_WithNonExistingId_ReturnsFalse()
    {
        // Act
        var result = _eventService.DeleteEvent(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Фильтрация по названию

    [Fact]
    public void GetEvents_WithTitleFilter_ReturnsMatchingEvents()
    {
        // Arrange
        _eventService.CreateEvent("Team Meeting", null, DateTime.Now, DateTime.Now.AddHours(1));
        _eventService.CreateEvent("Team Project", null, DateTime.Now, DateTime.Now.AddHours(1));
        _eventService.CreateEvent("Lunch", null, DateTime.Now, DateTime.Now.AddHours(1));

        // Act
        var result = _eventService.GetEvents(title: "Team");

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, e => Assert.Contains("Team", e.Title));
    }

    [Fact]
    public void GetEvents_WithTitleFilter_CaseInsensitive()
    {
        // Arrange
        _eventService.CreateEvent("Meeting", null, DateTime.Now, DateTime.Now.AddHours(1));

        // Act
        var result = _eventService.GetEvents(title: "meeting");

        // Assert
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public void GetEvents_WithTitleFilter_PartialMatch()
    {
        // Arrange
        _eventService.CreateEvent("Team Meeting Today", null, DateTime.Now, DateTime.Now.AddHours(1));

        // Act
        var result = _eventService.GetEvents(title: "Meet");

        // Assert
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public void GetEvents_WithEmptyTitleFilter_ReturnsAllEvents()
    {
        // Arrange
        _eventService.CreateEvent("Event 1", null, DateTime.Now, DateTime.Now.AddHours(1));
        _eventService.CreateEvent("Event 2", null, DateTime.Now, DateTime.Now.AddHours(1));

        // Act
        var result = _eventService.GetEvents(title: "");

        // Assert
        Assert.Equal(2, result.TotalCount);
    }

    #endregion

    #region Фильтрация по датам

    [Fact]
    public void GetEvents_WithFromDateFilter_ReturnsEventsStartingAfter()
    {
        // Arrange
        var baseDate = new DateTime(2026, 1, 1);
        _eventService.CreateEvent("Past", null, baseDate.AddDays(-1), baseDate.AddDays(-1).AddHours(1));
        _eventService.CreateEvent("Future", null, baseDate.AddDays(1), baseDate.AddDays(1).AddHours(1));

        // Act
        var result = _eventService.GetEvents(from: baseDate);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Future", result.Items.First().Title);
    }

    [Fact]
    public void GetEvents_WithToDateFilter_ReturnsEventsEndingBefore()
    {
        // Arrange
        var baseDate = new DateTime(2026, 1, 15);
        _eventService.CreateEvent("Early", null, baseDate.AddDays(-5), baseDate.AddDays(-5).AddHours(1));
        _eventService.CreateEvent("Late", null, baseDate.AddDays(5), baseDate.AddDays(5).AddHours(1));

        // Act
        var result = _eventService.GetEvents(to: baseDate);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Early", result.Items.First().Title);
    }

    [Fact]
    public void GetEvents_WithDateRangeFilter_ReturnsEventsInRange()
    {
        // Arrange
        var fromDate = new DateTime(2026, 1, 1);
        var toDate = new DateTime(2026, 1, 31);
        
        _eventService.CreateEvent("Before", null, fromDate.AddDays(-5), fromDate.AddDays(-5).AddHours(1));
        _eventService.CreateEvent("In Range", null, fromDate.AddDays(10), fromDate.AddDays(10).AddHours(1));
        _eventService.CreateEvent("After", null, toDate.AddDays(5), toDate.AddDays(5).AddHours(1));

        // Act
        var result = _eventService.GetEvents(from: fromDate, to: toDate);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("In Range", result.Items.First().Title);
    }

    #endregion

    #region Пагинация

    [Fact]
    public void GetEvents_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 25; i++)
        {
            _eventService.CreateEvent($"Event {i}", null, DateTime.Now, DateTime.Now.AddHours(1));
        }

        // Act
        var result = _eventService.GetEvents(page: 2, pageSize: 10);

        // Assert
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(2, result.Page);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void GetEvents_WithLastPage_ReturnsRemainingItems()
    {
        // Arrange
        for (int i = 1; i <= 25; i++)
        {
            _eventService.CreateEvent($"Event {i}", null, DateTime.Now, DateTime.Now.AddHours(1));
        }

        // Act
        var result = _eventService.GetEvents(page: 3, pageSize: 10);

        // Assert
        Assert.Equal(5, result.Items.Count()); // Оставшиеся 5 элементов
    }

    [Fact]
    public void GetEvents_WithPageBeyondRange_ReturnsEmptyList()
    {
        // Arrange
        _eventService.CreateEvent("Event 1", null, DateTime.Now, DateTime.Now.AddHours(1));

        // Act
        var result = _eventService.GetEvents(page: 10, pageSize: 10);

        // Assert
        Assert.Empty(result.Items);
    }

    #endregion

    #region Комбинированная фильтрация

    [Fact]
    public void GetEvents_WithCombinedFilters_ReturnsMatchingEvents()
    {
        // Arrange
        var baseDate = new DateTime(2026, 6, 1);
        
        _eventService.CreateEvent("Meeting", null, baseDate.AddDays(5), baseDate.AddDays(5).AddHours(1));
        _eventService.CreateEvent("Client Meeting", null, baseDate.AddDays(10), baseDate.AddDays(10).AddHours(1));
        _eventService.CreateEvent("Lunch", null, baseDate.AddDays(-5), baseDate.AddDays(-5).AddHours(1)); // До from
        _eventService.CreateEvent("Review", null, baseDate.AddDays(5), baseDate.AddDays(5).AddHours(1)); // Не содержит "Meeting"

        // Act
        var result = _eventService.GetEvents(
            title: "Meeting",
            from: baseDate,
            to: baseDate.AddDays(15)
        );

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, e => Assert.Contains("Meeting", e.Title));
    }

    #endregion

    #region Edge cases фильтрации

    [Fact]
    public void GetEvents_WithWhitespaceTitleFilter_ReturnsAllEvents()
    {
        // Arrange
        _eventService.CreateEvent("Event 1", null, DateTime.Now, DateTime.Now.AddHours(1));
        _eventService.CreateEvent("Event 2", null, DateTime.Now, DateTime.Now.AddHours(1));

        // Act
        var result = _eventService.GetEvents(title: "   ");

        // Assert
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public void GetEvents_WithNonMatchingTitleFilter_ReturnsEmptyResult()
    {
        // Arrange
        _eventService.CreateEvent("Meeting", null, DateTime.Now, DateTime.Now.AddHours(1));
        _eventService.CreateEvent("Project", null, DateTime.Now, DateTime.Now.AddHours(1));

        // Act
        var result = _eventService.GetEvents(title: "NonExistent");

        // Assert
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public void GetEvents_WithFromGreaterThanTo_ReturnsEmptyResult()
    {
        // Arrange
        var baseDate = new DateTime(2026, 1, 15);
        _eventService.CreateEvent("Event", null, baseDate, baseDate.AddHours(1));

        // Act - from позже to, такой фильтр логически невозможен
        var result = _eventService.GetEvents(
            from: baseDate.AddDays(10), 
            to: baseDate.AddDays(-10)
        );

        // Assert - должно вернуть пустой результат, т.к. нет событий, 
        // которые начинаются после from и заканчиваются до to при from > to
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public void GetEvents_WithBoundaryDates_ReturnsCorrectEvents()
    {
        // Arrange
        var exactDate = new DateTime(2026, 6, 15, 10, 0, 0);
        _eventService.CreateEvent("Exact Start", null, exactDate, exactDate.AddHours(2));
        _eventService.CreateEvent("Exact End", null, exactDate.AddHours(-2), exactDate);

        // Act - фильтр включает граничные значения
        var resultFrom = _eventService.GetEvents(from: exactDate);
        var resultTo = _eventService.GetEvents(to: exactDate);

        // Assert
        Assert.Single(resultFrom.Items);
        Assert.Equal("Exact Start", resultFrom.Items.First().Title);
        
        Assert.Single(resultTo.Items);
        Assert.Equal("Exact End", resultTo.Items.First().Title);
    }

    #endregion
}
