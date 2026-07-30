using EventTracker.EventsService.Application.DTOs;
using EventTracker.EventsService.Application.Options;
using EventTracker.EventsService.Application.Ports;
using EventTracker.EventsService.Application.Services;
using EventTracker.EventsService.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EventTracker.EventsService.Tests;

public class EventServiceCacheTests
{
    private static readonly DateTime StartAt = new(2026, 8, 1, 18, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime EndAt = new(2026, 8, 1, 21, 0, 0, DateTimeKind.Utc);

    private static Event CreateEvent()
    {
        return new Event("Concert", "Test concert", StartAt, EndAt, 100);
    }

    private static EventService CreateService(
        IEventRepository repository,
        ICacheService cache,
        CacheOptions? options = null)
    {
        var loggerMock = new Mock<ILogger<EventService>>();
        var optionsMock = new Mock<IOptions<CacheOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options ?? new CacheOptions { EventTtlSeconds = 300, TopEventsTtlSeconds = 60 });

        return new EventService(repository, cache, optionsMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task GetEventByIdAsync_CacheHit_ReturnsFromCacheAndDoesNotCallRepository()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var cachedDto = new EventResponseDto(eventId, "Cached", null, StartAt, EndAt, 100, 99);

        var repoMock = new Mock<IEventRepository>();
        var cacheMock = new Mock<ICacheService>();
        cacheMock
            .Setup(c => c.GetAsync<EventResponseDto>(CacheKeys.Event(eventId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedDto);

        var service = CreateService(repoMock.Object, cacheMock.Object);

        // Act
        var result = await service.GetEventByIdAsync(eventId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(eventId, result.Id);
        Assert.Equal("Cached", result.Title);
        repoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        cacheMock.Verify(c => c.GetAsync<EventResponseDto>(CacheKeys.Event(eventId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEventByIdAsync_CacheMiss_FetchesFromRepositoryAndStoresInCache()
    {
        // Arrange
        var ev = CreateEvent();
        var repoMock = new Mock<IEventRepository>();
        repoMock.Setup(r => r.GetByIdAsync(ev.Id)).ReturnsAsync(ev);

        var cacheMock = new Mock<ICacheService>();
        cacheMock
            .Setup(c => c.GetAsync<EventResponseDto>(CacheKeys.Event(ev.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventResponseDto?)null);

        var service = CreateService(repoMock.Object, cacheMock.Object);

        // Act
        var result = await service.GetEventByIdAsync(ev.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ev.Id, result.Id);
        Assert.Equal(ev.Title, result.Title);
        repoMock.Verify(r => r.GetByIdAsync(ev.Id), Times.Once);
        cacheMock.Verify(
            c => c.SetAsync(
                CacheKeys.Event(ev.Id),
                It.Is<EventResponseDto>(d => d.Id == ev.Id),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateEventAsync_SavesToDatabaseAndStoresInCache()
    {
        // Arrange
        var repoMock = new Mock<IEventRepository>();
        var cacheMock = new Mock<ICacheService>();
        var service = CreateService(repoMock.Object, cacheMock.Object);

        // Act
        var created = await service.CreateEventAsync("Concert", null, StartAt, EndAt, 100);

        // Assert
        repoMock.Verify(r => r.AddAsync(It.Is<Event>(e => e.Id == created.Id)), Times.Once);
        repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        cacheMock.Verify(
            c => c.SetAsync(
                CacheKeys.Event(created.Id),
                It.Is<EventResponseDto>(d => d.Id == created.Id && d.TotalSeats == 100),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateEventAsync_SavesToDatabaseAndInvalidatesCache()
    {
        // Arrange
        var ev = CreateEvent();
        var repoMock = new Mock<IEventRepository>();
        repoMock.Setup(r => r.GetByIdAsync(ev.Id)).ReturnsAsync(ev);

        var cacheMock = new Mock<ICacheService>();
        var service = CreateService(repoMock.Object, cacheMock.Object);

        // Act
        var updated = await service.UpdateEventAsync(ev.Id, "Updated", "New description", StartAt, EndAt.AddHours(1));

        // Assert
        Assert.NotNull(updated);
        repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        cacheMock.Verify(c => c.RemoveAsync(CacheKeys.Event(ev.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteEventAsync_RemovesFromDatabaseAndInvalidatesCache()
    {
        // Arrange
        var ev = CreateEvent();
        var repoMock = new Mock<IEventRepository>();
        repoMock.Setup(r => r.GetByIdAsync(ev.Id)).ReturnsAsync(ev);

        var cacheMock = new Mock<ICacheService>();
        var service = CreateService(repoMock.Object, cacheMock.Object);

        // Act
        var deleted = await service.DeleteEventAsync(ev.Id);

        // Assert
        Assert.True(deleted);
        repoMock.Verify(r => r.Remove(ev), Times.Once);
        repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        cacheMock.Verify(c => c.RemoveAsync(CacheKeys.Event(ev.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTopEventsAsync_CacheHit_ReturnsFromCacheAndDoesNotCallRepository()
    {
        // Arrange
        var cached = new List<EventResponseDto>
        {
            new(Guid.NewGuid(), "Top 1", null, StartAt, EndAt, 100, 10),
            new(Guid.NewGuid(), "Top 2", null, StartAt, EndAt, 100, 20)
        };

        var repoMock = new Mock<IEventRepository>();
        var cacheMock = new Mock<ICacheService>();
        cacheMock
            .Setup(c => c.GetAsync<List<EventResponseDto>>(CacheKeys.TopEvents, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var service = CreateService(repoMock.Object, cacheMock.Object);

        // Act
        var result = await service.GetTopEventsAsync(10);

        // Assert
        Assert.Equal(2, result.Count);
        repoMock.Verify(r => r.GetTopEventsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTopEventsAsync_CacheMiss_FetchesFromRepositoryAndStoresInCache()
    {
        // Arrange
        var events = new List<Event>
        {
            new("Top 1", null, StartAt, EndAt, 100),
            new("Top 2", null, StartAt, EndAt, 100)
        };

        var repoMock = new Mock<IEventRepository>();
        repoMock.Setup(r => r.GetTopEventsAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(events);

        var cacheMock = new Mock<ICacheService>();
        cacheMock
            .Setup(c => c.GetAsync<List<EventResponseDto>>(CacheKeys.TopEvents, It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<EventResponseDto>?)null);

        var service = CreateService(repoMock.Object, cacheMock.Object);

        // Act
        var result = await service.GetTopEventsAsync(10);

        // Assert
        Assert.Equal(2, result.Count);
        repoMock.Verify(r => r.GetTopEventsAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        cacheMock.Verify(
            c => c.SetAsync(
                CacheKeys.TopEvents,
                It.Is<List<EventResponseDto>>(list => list.Count == 2),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
