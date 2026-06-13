using EventTrackerApi.Domain.Models;
using EventTrackerApi.Application.Ports;
using EventTrackerApi.Application.Services;
using EventTrackerApi.Infrastructure.DataAccess;
using EventTrackerApi.Infrastructure.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventTrackerApi.Tests;

public class EventServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly EventService _eventService;

    public EventServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IEventService, EventService>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _eventService = (EventService)_scope.ServiceProvider.GetRequiredService<IEventService>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
    }

    #region Создание события

    [Fact]
    public async Task CreateEventAsync_WithValidData_ReturnsCreatedEvent()
    {
        // Arrange
        var title = "Test Event";
        var description = "Test Description";
        var startAt = DateTime.Now;
        var endAt = DateTime.Now.AddHours(1);
        var totalSeats = 100;

        // Act
        var result = await _eventService.CreateEventAsync(title, description, startAt, endAt, totalSeats);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(title, result.Title);
        Assert.Equal(description, result.Description);
        Assert.Equal(startAt.ToUniversalTime(), result.StartAt);
        Assert.Equal(endAt.ToUniversalTime(), result.EndAt);
        Assert.Equal(totalSeats, result.TotalSeats);
        Assert.Equal(totalSeats, result.AvailableSeats);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Theory]
    [InlineData("", "Description", "2026-01-01", "2026-01-02", 10)]
    [InlineData("Title", "Description", "2026-01-05", "2026-01-01", 10)]
    [InlineData("Title", "Description", "2026-01-01", "2026-01-02", 0)]
    [InlineData("Title", "Description", "2026-01-01", "2026-01-02", -1)]
    public async Task CreateEventAsync_WithInvalidData_ThrowsArgumentException(string title, string? description, string startAtStr, string endAtStr, int totalSeats)
    {
        // Arrange
        var startAt = DateTime.Parse(startAtStr);
        var endAt = DateTime.Parse(endAtStr);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _eventService.CreateEventAsync(title, description, startAt, endAt, totalSeats));
    }

    #endregion

    #region Получение всех событий

    [Fact]
    public async Task GetEventsAsync_WithNoEvents_ReturnsEmptyPaginatedResult()
    {
        // Act
        var result = await _eventService.GetEventsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetEventsAsync_WithMultipleEvents_ReturnsAllEvents()
    {
        // Arrange
        await _eventService.CreateEventAsync("Event 1", null, DateTime.Now, DateTime.Now.AddHours(1), 10);
        await _eventService.CreateEventAsync("Event 2", null, DateTime.Now, DateTime.Now.AddHours(1), 10);
        await _eventService.CreateEventAsync("Event 3", null, DateTime.Now, DateTime.Now.AddHours(1), 10);

        // Act
        var result = await _eventService.GetEventsAsync();

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count());
    }

    #endregion

    #region Получение события по ID

    [Fact]
    public async Task GetEventByIdAsync_WithExistingId_ReturnsEvent()
    {
        // Arrange
        var createdEvent = await _eventService.CreateEventAsync("Test", null, DateTime.Now, DateTime.Now.AddHours(1), 10);

        // Act
        var result = await _eventService.GetEventByIdAsync(createdEvent.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdEvent.Id, result.Id);
        Assert.Equal(createdEvent.Title, result.Title);
    }

    [Fact]
    public async Task GetEventByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = await _eventService.GetEventByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Обновление события

    [Fact]
    public async Task UpdateEventAsync_WithExistingId_ReturnsUpdatedEvent()
    {
        // Arrange
        var createdEvent = await _eventService.CreateEventAsync("Original", null, DateTime.Now, DateTime.Now.AddHours(1), 10);
        var newTitle = "Updated Title";
        var newDescription = "Updated Description";
        var newStartAt = DateTime.Now.AddDays(1);
        var newEndAt = DateTime.Now.AddDays(1).AddHours(1);

        // Act
        var result = await _eventService.UpdateEventAsync(createdEvent.Id, newTitle, newDescription, newStartAt, newEndAt);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newTitle, result.Title);
        Assert.Equal(newDescription, result.Description);
        Assert.Equal(newStartAt.ToUniversalTime(), result.StartAt);
        Assert.Equal(newEndAt.ToUniversalTime(), result.EndAt);
        Assert.Equal(createdEvent.TotalSeats, result.TotalSeats);
        Assert.Equal(createdEvent.AvailableSeats, result.AvailableSeats);
    }

    [Fact]
    public async Task UpdateEventAsync_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = await _eventService.UpdateEventAsync(Guid.NewGuid(), "Title", null, DateTime.Now, DateTime.Now.AddHours(1));

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("", "Description", "2026-01-01", "2026-01-02")]
    [InlineData("Title", "Description", "2026-01-05", "2026-01-01")]
    public async Task UpdateEventAsync_WithInvalidData_ThrowsArgumentException(string title, string? description, string startAtStr, string endAtStr)
    {
        // Arrange
        var createdEvent = await _eventService.CreateEventAsync("Original", null, DateTime.Now, DateTime.Now.AddHours(1), 10);
        var startAt = DateTime.Parse(startAtStr);
        var endAt = DateTime.Parse(endAtStr);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _eventService.UpdateEventAsync(createdEvent.Id, title, description, startAt, endAt));
    }

    #endregion

    #region Удаление события

    [Fact]
    public async Task DeleteEventAsync_WithExistingId_ReturnsTrue()
    {
        // Arrange
        var createdEvent = await _eventService.CreateEventAsync("Test", null, DateTime.Now, DateTime.Now.AddHours(1), 10);

        // Act
        var result = await _eventService.DeleteEventAsync(createdEvent.Id);

        // Assert
        Assert.True(result);
        Assert.Null(await _eventService.GetEventByIdAsync(createdEvent.Id));
    }

    [Fact]
    public async Task DeleteEventAsync_WithNonExistingId_ReturnsFalse()
    {
        // Act
        var result = await _eventService.DeleteEventAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Фильтрация по названию

    [Fact]
    public async Task GetEventsAsync_WithTitleFilter_ReturnsMatchingEvents()
    {
        // Arrange
        await _eventService.CreateEventAsync("Team Meeting", null, DateTime.Now, DateTime.Now.AddHours(1), 10);
        await _eventService.CreateEventAsync("Team Project", null, DateTime.Now, DateTime.Now.AddHours(1), 10);
        await _eventService.CreateEventAsync("Lunch", null, DateTime.Now, DateTime.Now.AddHours(1), 10);

        // Act
        var result = await _eventService.GetEventsAsync(title: "Team");

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, e => Assert.Contains("Team", e.Title));
    }

    [Fact]
    public async Task GetEventsAsync_WithTitleFilter_CaseInsensitive()
    {
        // Arrange
        await _eventService.CreateEventAsync("Meeting", null, DateTime.Now, DateTime.Now.AddHours(1), 10);

        // Act
        var result = await _eventService.GetEventsAsync(title: "meeting");

        // Assert
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetEventsAsync_WithTitleFilter_PartialMatch()
    {
        // Arrange
        await _eventService.CreateEventAsync("Team Meeting Today", null, DateTime.Now, DateTime.Now.AddHours(1), 10);

        // Act
        var result = await _eventService.GetEventsAsync(title: "Meet");

        // Assert
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetEventsAsync_WithEmptyTitleFilter_ReturnsAllEvents()
    {
        // Arrange
        await _eventService.CreateEventAsync("Event 1", null, DateTime.Now, DateTime.Now.AddHours(1), 10);
        await _eventService.CreateEventAsync("Event 2", null, DateTime.Now, DateTime.Now.AddHours(1), 10);

        // Act
        var result = await _eventService.GetEventsAsync(title: "");

        // Assert
        Assert.Equal(2, result.TotalCount);
    }

    #endregion

    #region Фильтрация по датам

    [Fact]
    public async Task GetEventsAsync_WithFromDateFilter_ReturnsEventsStartingAfter()
    {
        // Arrange
        var baseDate = new DateTime(2026, 1, 1);
        await _eventService.CreateEventAsync("Past", null, baseDate.AddDays(-1), baseDate.AddDays(-1).AddHours(1), 10);
        await _eventService.CreateEventAsync("Future", null, baseDate.AddDays(1), baseDate.AddDays(1).AddHours(1), 10);

        // Act
        var result = await _eventService.GetEventsAsync(from: baseDate);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Future", result.Items.First().Title);
    }

    [Fact]
    public async Task GetEventsAsync_WithToDateFilter_ReturnsEventsEndingBefore()
    {
        // Arrange
        var baseDate = new DateTime(2026, 1, 15);
        await _eventService.CreateEventAsync("Early", null, baseDate.AddDays(-5), baseDate.AddDays(-5).AddHours(1), 10);
        await _eventService.CreateEventAsync("Late", null, baseDate.AddDays(5), baseDate.AddDays(5).AddHours(1), 10);

        // Act
        var result = await _eventService.GetEventsAsync(to: baseDate);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Early", result.Items.First().Title);
    }

    [Fact]
    public async Task GetEventsAsync_WithDateRangeFilter_ReturnsEventsInRange()
    {
        // Arrange
        var fromDate = new DateTime(2026, 1, 1);
        var toDate = new DateTime(2026, 1, 31);
        
        await _eventService.CreateEventAsync("Before", null, fromDate.AddDays(-5), fromDate.AddDays(-5).AddHours(1), 10);
        await _eventService.CreateEventAsync("In Range", null, fromDate.AddDays(10), fromDate.AddDays(10).AddHours(1), 10);
        await _eventService.CreateEventAsync("After", null, toDate.AddDays(5), toDate.AddDays(5).AddHours(1), 10);

        // Act
        var result = await _eventService.GetEventsAsync(from: fromDate, to: toDate);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("In Range", result.Items.First().Title);
    }

    #endregion

    #region Пагинация

    [Fact]
    public async Task GetEventsAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 25; i++)
        {
            await _eventService.CreateEventAsync($"Event {i}", null, DateTime.Now, DateTime.Now.AddHours(1), 10);
        }

        // Act
        var result = await _eventService.GetEventsAsync(page: 2, pageSize: 10);

        // Assert
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(2, result.Page);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task GetEventsAsync_WithLastPage_ReturnsRemainingItems()
    {
        // Arrange
        for (int i = 1; i <= 25; i++)
        {
            await _eventService.CreateEventAsync($"Event {i}", null, DateTime.Now, DateTime.Now.AddHours(1), 10);
        }

        // Act
        var result = await _eventService.GetEventsAsync(page: 3, pageSize: 10);

        // Assert
        Assert.Equal(5, result.Items.Count()); // Оставшиеся 5 элементов
    }

    [Fact]
    public async Task GetEventsAsync_WithPageBeyondRange_ReturnsEmptyList()
    {
        // Arrange
        await _eventService.CreateEventAsync("Event 1", null, DateTime.Now, DateTime.Now.AddHours(1), 10);

        // Act
        var result = await _eventService.GetEventsAsync(page: 10, pageSize: 10);

        // Assert
        Assert.Empty(result.Items);
    }

    #endregion

    #region Комбинированная фильтрация

    [Fact]
    public async Task GetEventsAsync_WithCombinedFilters_ReturnsMatchingEvents()
    {
        // Arrange
        var baseDate = new DateTime(2026, 6, 1);
        
        await _eventService.CreateEventAsync("Meeting", null, baseDate.AddDays(5), baseDate.AddDays(5).AddHours(1), 10);
        await _eventService.CreateEventAsync("Client Meeting", null, baseDate.AddDays(10), baseDate.AddDays(10).AddHours(1), 10);
        await _eventService.CreateEventAsync("Lunch", null, baseDate.AddDays(-5), baseDate.AddDays(-5).AddHours(1), 10); // До from
        await _eventService.CreateEventAsync("Review", null, baseDate.AddDays(5), baseDate.AddDays(5).AddHours(1), 10); // Не содержит "Meeting"

        // Act
        var result = await _eventService.GetEventsAsync(
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
    public async Task GetEventsAsync_WithWhitespaceTitleFilter_ReturnsAllEvents()
    {
        // Arrange
        await _eventService.CreateEventAsync("Event 1", null, DateTime.Now, DateTime.Now.AddHours(1), 10);
        await _eventService.CreateEventAsync("Event 2", null, DateTime.Now, DateTime.Now.AddHours(1), 10);

        // Act
        var result = await _eventService.GetEventsAsync(title: "   ");

        // Assert
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetEventsAsync_WithNonMatchingTitleFilter_ReturnsEmptyResult()
    {
        // Arrange
        await _eventService.CreateEventAsync("Meeting", null, DateTime.Now, DateTime.Now.AddHours(1), 10);
        await _eventService.CreateEventAsync("Project", null, DateTime.Now, DateTime.Now.AddHours(1), 10);

        // Act
        var result = await _eventService.GetEventsAsync(title: "NonExistent");

        // Assert
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetEventsAsync_WithFromGreaterThanTo_ReturnsEmptyResult()
    {
        // Arrange
        var baseDate = new DateTime(2026, 1, 15);
        await _eventService.CreateEventAsync("Event", null, baseDate, baseDate.AddHours(1), 10);

        // Act - from позже to, такой фильтр логически невозможен
        var result = await _eventService.GetEventsAsync(
            from: baseDate.AddDays(10), 
            to: baseDate.AddDays(-10)
        );

        // Assert - должно вернуть пустой результат, т.к. нет событий, 
        // которые начинаются после from и заканчиваются до to при from > to
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetEventsAsync_WithBoundaryDates_ReturnsCorrectEvents()
    {
        // Arrange
        var exactDate = new DateTime(2026, 6, 15, 10, 0, 0);
        await _eventService.CreateEventAsync("Exact Start", null, exactDate, exactDate.AddHours(2), 10);
        await _eventService.CreateEventAsync("Exact End", null, exactDate.AddHours(-2), exactDate, 10);

        // Act - фильтр включает граничные значения
        var resultFrom = await _eventService.GetEventsAsync(from: exactDate);
        var resultTo = await _eventService.GetEventsAsync(to: exactDate);

        // Assert
        Assert.Single(resultFrom.Items);
        Assert.Equal("Exact Start", resultFrom.Items.First().Title);
        
        Assert.Single(resultTo.Items);
        Assert.Equal("Exact End", resultTo.Items.First().Title);
    }

    #endregion
}
