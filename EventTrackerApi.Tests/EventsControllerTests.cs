using EventTrackerApi.Controllers;
using EventTrackerApi.Infrastructure.Mappers;
using EventTrackerApi.Models;
using EventTrackerApi.Models.Dto;
using EventTrackerApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventTrackerApi.Tests;

public class EventsControllerTests
{
    private readonly Mock<IEventService> _eventServiceMock;
    private readonly Mock<ILogger<EventsController>> _loggerMock;
    private readonly EventsController _controller;

    public EventsControllerTests()
    {
        _eventServiceMock = new Mock<IEventService>();
        _loggerMock = new Mock<ILogger<EventsController>>();
        _controller = new EventsController(_eventServiceMock.Object);
    }

    #region GET /events - Получение списка с фильтрацией и пагинацией

    [Fact]
    public void GetEvents_WithDefaultParameters_ReturnsPaginatedResult()
    {
        // Arrange
        var events = new List<Event>
        {
            new("Event 1", null, DateTime.Now, DateTime.Now.AddHours(1)),
            new("Event 2", null, DateTime.Now, DateTime.Now.AddHours(1))
        };
        
        var paginatedResult = new PaginatedResult<Event>
        {
            TotalCount = 2,
            Items = events,
            Page = 1,
            PageSize = 10
        };

        _eventServiceMock
            .Setup(s => s.GetEvents(null, null, null, 1, 10))
            .Returns(paginatedResult);

        // Act
        var result = _controller.GetEvents();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResult<EventResponseDto>>(okResult.Value);
        Assert.Equal(2, response.TotalCount);
        Assert.Equal(2, response.Items.Count());
        
        _eventServiceMock.Verify(s => s.GetEvents(null, null, null, 1, 10), Times.Once);
    }

    [Fact]
    public void GetEvents_WithTitleFilter_ReturnsFilteredEvents()
    {
        // Arrange
        var titleFilter = "Meeting";
        var events = new List<Event>
        {
            new("Team Meeting", null, DateTime.Now, DateTime.Now.AddHours(1))
        };
        
        var paginatedResult = new PaginatedResult<Event>
        {
            TotalCount = 1,
            Items = events,
            Page = 1,
            PageSize = 10
        };

        _eventServiceMock
            .Setup(s => s.GetEvents(titleFilter, null, null, 1, 10))
            .Returns(paginatedResult);

        // Act
        var result = _controller.GetEvents(title: titleFilter);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResult<EventResponseDto>>(okResult.Value);
        Assert.Single(response.Items);
        
        _eventServiceMock.Verify(s => s.GetEvents(titleFilter, null, null, 1, 10), Times.Once);
    }

    [Fact]
    public void GetEvents_WithDateFilters_ReturnsFilteredEvents()
    {
        // Arrange
        var fromDate = new DateTime(2024, 1, 1);
        var toDate = new DateTime(2024, 12, 31);
        var events = new List<Event>
        {
            new("Event", null, fromDate.AddMonths(1), fromDate.AddMonths(1).AddHours(1))
        };
        
        var paginatedResult = new PaginatedResult<Event>
        {
            TotalCount = 1,
            Items = events,
            Page = 1,
            PageSize = 10
        };

        _eventServiceMock
            .Setup(s => s.GetEvents(null, fromDate, toDate, 1, 10))
            .Returns(paginatedResult);

        // Act
        var result = _controller.GetEvents(from: fromDate, to: toDate);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<PaginatedResult<EventResponseDto>>(okResult.Value);
        
        _eventServiceMock.Verify(s => s.GetEvents(null, fromDate, toDate, 1, 10), Times.Once);
    }

    [Fact]
    public void GetEvents_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var page = 2;
        var pageSize = 5;
        var events = new List<Event>
        {
            new("Event 6", null, DateTime.Now, DateTime.Now.AddHours(1))
        };
        
        var paginatedResult = new PaginatedResult<Event>
        {
            TotalCount = 6,
            Items = events,
            Page = page,
            PageSize = pageSize
        };

        _eventServiceMock
            .Setup(s => s.GetEvents(null, null, null, page, pageSize))
            .Returns(paginatedResult);

        // Act
        var result = _controller.GetEvents(page: page, pageSize: pageSize);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResult<EventResponseDto>>(okResult.Value);
        Assert.Equal(page, response.Page);
        Assert.Equal(pageSize, response.PageSize);
        Assert.Single(response.Items);
    }

    [Fact]
    public void GetEvents_WithCombinedFilters_ReturnsFilteredPaginatedEvents()
    {
        // Arrange
        var title = "Meeting";
        var fromDate = new DateTime(2024, 1, 1);
        var toDate = new DateTime(2024, 12, 31);
        var page = 1;
        var pageSize = 10;
        
        var events = new List<Event>
        {
            new("Team Meeting", null, fromDate.AddMonths(1), fromDate.AddMonths(1).AddHours(1))
        };
        
        var paginatedResult = new PaginatedResult<Event>
        {
            TotalCount = 1,
            Items = events,
            Page = page,
            PageSize = pageSize
        };

        _eventServiceMock
            .Setup(s => s.GetEvents(title, fromDate, toDate, page, pageSize))
            .Returns(paginatedResult);

        // Act
        var result = _controller.GetEvents(title, fromDate, toDate, page, pageSize);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginatedResult<EventResponseDto>>(okResult.Value);
        Assert.Single(response.Items);
        
        _eventServiceMock.Verify(s => s.GetEvents(title, fromDate, toDate, page, pageSize), Times.Once);
    }

    #endregion

    #region GET /events/{id} - Получение по ID

    [Fact]
    public void GetEventById_WithExistingId_ReturnsOk()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var ev = new Event("Test Event", null, DateTime.Now, DateTime.Now.AddHours(1));
        
        _eventServiceMock
            .Setup(s => s.GetEventById(eventId))
            .Returns(ev);

        // Act
        var result = _controller.GetEventById(eventId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<EventResponseDto>(okResult.Value);
        Assert.Equal(ev.Title, response.Title);
        
        _eventServiceMock.Verify(s => s.GetEventById(eventId), Times.Once);
    }

    [Fact]
    public void GetEventById_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        
        _eventServiceMock
            .Setup(s => s.GetEventById(eventId))
            .Returns((Event?)null);

        // Act
        var result = _controller.GetEventById(eventId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        
        _eventServiceMock.Verify(s => s.GetEventById(eventId), Times.Once);
    }

    #endregion

    #region POST /events - Создание

    [Fact]
    public void CreateEvent_WithValidData_ReturnsCreatedAtAction()
    {
        // Arrange
        var dto = new CreateEventDto(
            "New Event",
            "Description",
            DateTime.Now,
            DateTime.Now.AddHours(1)
        );
        
        var createdEvent = new Event(dto.Title, dto.Description, dto.StartAt, dto.EndAt);
        
        _eventServiceMock
            .Setup(s => s.CreateEvent(dto.Title, dto.Description, dto.StartAt, dto.EndAt))
            .Returns(createdEvent);

        // Act
        var result = _controller.CreateEvent(dto);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(EventsController.GetEventById), createdAtActionResult.ActionName);
        Assert.Equal(createdEvent.Id, ((EventResponseDto)createdAtActionResult.Value!).Id);
        
        _eventServiceMock.Verify(s => s.CreateEvent(dto.Title, dto.Description, dto.StartAt, dto.EndAt), Times.Once);
    }

    #endregion

    #region PUT /events/{id} - Обновление

    [Fact]
    public void UpdateEvent_WithExistingId_ReturnsOk()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var dto = new UpdateEventDto(
            "Updated Event",
            "Updated Description",
            DateTime.Now,
            DateTime.Now.AddHours(2)
        );
        
        var updatedEvent = new Event(eventId, dto.Title, dto.Description, dto.StartAt, dto.EndAt);
        
        _eventServiceMock
            .Setup(s => s.UpdateEvent(eventId, dto.Title, dto.Description, dto.StartAt, dto.EndAt))
            .Returns(updatedEvent);

        // Act
        var result = _controller.UpdateEvent(eventId, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<EventResponseDto>(okResult.Value);
        Assert.Equal(dto.Title, response.Title);
        
        _eventServiceMock.Verify(s => s.UpdateEvent(eventId, dto.Title, dto.Description, dto.StartAt, dto.EndAt), Times.Once);
    }

    [Fact]
    public void UpdateEvent_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var dto = new UpdateEventDto(
            "Updated Event",
            null,
            DateTime.Now,
            DateTime.Now.AddHours(1)
        );
        
        _eventServiceMock
            .Setup(s => s.UpdateEvent(eventId, dto.Title, dto.Description, dto.StartAt, dto.EndAt))
            .Returns((Event?)null);

        // Act
        var result = _controller.UpdateEvent(eventId, dto);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        
        _eventServiceMock.Verify(s => s.UpdateEvent(eventId, dto.Title, dto.Description, dto.StartAt, dto.EndAt), Times.Once);
    }

    #endregion

    #region DELETE /events/{id} - Удаление

    [Fact]
    public void DeleteEvent_WithExistingId_ReturnsNoContent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        
        _eventServiceMock
            .Setup(s => s.DeleteEvent(eventId))
            .Returns(true);

        // Act
        var result = _controller.DeleteEvent(eventId);

        // Assert
        Assert.IsType<NoContentResult>(result);
        
        _eventServiceMock.Verify(s => s.DeleteEvent(eventId), Times.Once);
    }

    [Fact]
    public void DeleteEvent_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        
        _eventServiceMock
            .Setup(s => s.DeleteEvent(eventId))
            .Returns(false);

        // Act
        var result = _controller.DeleteEvent(eventId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        
        _eventServiceMock.Verify(s => s.DeleteEvent(eventId), Times.Once);
    }

    #endregion
}
