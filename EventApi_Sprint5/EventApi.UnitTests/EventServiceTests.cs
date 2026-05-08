namespace EventApi.UnitTests;

using EventApi.DataAccess;
using EventApi.Dto;
using EventApi.Exceptions;
using EventApi.Services;
using EventApi.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public sealed class EventServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly IEventService _eventService;

    public EventServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddScoped<IEventService, EventService>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _eventService = _scope.ServiceProvider.GetRequiredService<IEventService>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
    }

    #region CreateEventAsync Tests

    [Fact]
    public async Task CreateEventAsync_WithValidData_ReturnsEventInfo()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        var createEvent = new CreateEvent
        {
            Title = "Test Event",
            Description = "Test Description",
            StartAt = futureDate,
            EndAt = futureDate.AddHours(2),
            TotalSeats = 10,
        };

        var result = await _eventService.CreateEventAsync(createEvent);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Test Event", result.Title);
        Assert.Equal("Test Description", result.Description);
        Assert.Equal(futureDate, result.StartAt);
        Assert.Equal(futureDate.AddHours(2), result.EndAt);
    }

    [Fact]
    public async Task CreateEventAsync_WithNullTitle_ThrowsValidationException()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        var createEvent = new CreateEvent
        {
            Title = null,
            StartAt = futureDate,
            EndAt = futureDate.AddHours(2),
            TotalSeats = 10,
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _eventService.CreateEventAsync(createEvent));
        Assert.Contains("Title", exception.Errors.Keys);
    }

    [Fact]
    public async Task CreateEventAsync_WithEmptyTitle_ThrowsValidationException()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        var createEvent = new CreateEvent
        {
            Title = "   ",
            StartAt = futureDate,
            EndAt = futureDate.AddHours(2),
            TotalSeats = 10,
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _eventService.CreateEventAsync(createEvent));
        Assert.Contains("Title", exception.Errors.Keys);
    }

    [Fact]
    public async Task CreateEventAsync_WithNullStartAt_ThrowsValidationException()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        var createEvent = new CreateEvent
        {
            Title = "Test Event",
            StartAt = null,
            EndAt = futureDate.AddHours(2),
            TotalSeats = 10,
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _eventService.CreateEventAsync(createEvent));
        Assert.Contains("StartAt", exception.Errors.Keys);
    }

    [Fact]
    public async Task CreateEventAsync_WithNullEndAt_ThrowsValidationException()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        var createEvent = new CreateEvent
        {
            Title = "Test Event",
            StartAt = futureDate,
            EndAt = null,
            TotalSeats = 10,
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _eventService.CreateEventAsync(createEvent));
        Assert.Contains("EndAt", exception.Errors.Keys);
    }

    [Fact]
    public async Task CreateEventAsync_WithPastStartAt_ThrowsValidationException()
    {
        var pastDate = DateTime.UtcNow.AddDays(-1);
        var createEvent = new CreateEvent
        {
            Title = "Test Event",
            StartAt = pastDate,
            EndAt = pastDate.AddHours(2),
            TotalSeats = 10,
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _eventService.CreateEventAsync(createEvent));
        Assert.Contains("StartAt", exception.Errors.Keys);
    }

    [Fact]
    public async Task CreateEventAsync_WithEndAtBeforeStartAt_ThrowsValidationException()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        var createEvent = new CreateEvent
        {
            Title = "Test Event",
            StartAt = futureDate,
            EndAt = futureDate.AddHours(-1),
            TotalSeats = 10,
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _eventService.CreateEventAsync(createEvent));
        Assert.Contains("EndAt", exception.Errors.Keys);
    }

    [Fact]
    public async Task CreateEventAsync_WithEndAtEqualToStartAt_ThrowsValidationException()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        var createEvent = new CreateEvent
        {
            Title = "Test Event",
            StartAt = futureDate,
            EndAt = futureDate,
            TotalSeats = 10,
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _eventService.CreateEventAsync(createEvent));
        Assert.Contains("EndAt", exception.Errors.Keys);
    }

    [Fact]
    public async Task CreateEventAsync_WithTitleWhitespace_TrimsTitleAndCreatesEvent()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        var createEvent = new CreateEvent
        {
            Title = "  Test Event  ",
            StartAt = futureDate,
            EndAt = futureDate.AddHours(2),
            TotalSeats = 10,
        };

        var result = await _eventService.CreateEventAsync(createEvent);

        Assert.Equal("Test Event", result.Title);
    }

    #endregion

    #region GetEventByIdAsync Tests

    [Fact]
    public async Task GetEventByIdAsync_WithValidId_ReturnsEventInfo()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        var createdEvent = await _eventService.CreateEventAsync(new CreateEvent
        {
            Title = "Test Event",
            Description = "Test Description",
            StartAt = futureDate,
            EndAt = futureDate.AddHours(2),
            TotalSeats = 10,
        });

        var result = await _eventService.GetEventByIdAsync(createdEvent.Id);

        Assert.NotNull(result);
        Assert.Equal(createdEvent.Id, result.Id);
        Assert.Equal("Test Event", result.Title);
    }

    [Fact]
    public async Task GetEventByIdAsync_WithInvalidId_ThrowsNotFoundException()
    {
        var invalidId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _eventService.GetEventByIdAsync(invalidId));
        Assert.Equal("Event not found", exception.Message);
    }

    #endregion

    #region GetAllEventsAsync Tests

    [Fact]
    public async Task GetAllEventsAsync_WithNoEvents_ReturnsEmptyArray()
    {
        var result = await _eventService.GetAllEventsAsync();

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetAllEventsAsync_WithMultipleEvents_ReturnsAllEvents()
    {
        var futureDate1 = DateTime.UtcNow.AddDays(1);
        var futureDate2 = DateTime.UtcNow.AddDays(2);

        await _eventService.CreateEventAsync(new CreateEvent
        {
            Title = "Event 1",
            StartAt = futureDate1,
            EndAt = futureDate1.AddHours(2),
            TotalSeats = 10,
        });

        await _eventService.CreateEventAsync(new CreateEvent
        {
            Title = "Event 2",
            StartAt = futureDate2,
            EndAt = futureDate2.AddHours(2),
            TotalSeats = 10,
        });

        var result = await _eventService.GetAllEventsAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Length);
    }

    [Fact]
    public async Task GetAllEventsAsync_WithFromFilter_ReturnsFilteredEvents()
    {
        var futureDate1 = DateTime.UtcNow.AddDays(1);
        var futureDate2 = DateTime.UtcNow.AddDays(2);
        var filterDate = futureDate1.AddHours(1);

        await _eventService.CreateEventAsync(new CreateEvent
        {
            Title = "Event 1",
            StartAt = futureDate1,
            EndAt = futureDate1.AddHours(2),
            TotalSeats = 10,
        });

        await _eventService.CreateEventAsync(new CreateEvent
        {
            Title = "Event 2",
            StartAt = futureDate2,
            EndAt = futureDate2.AddHours(2),
            TotalSeats = 10,
        });

        var result = await _eventService.GetAllEventsAsync(from: filterDate);

        Assert.Single(result.Items);
        Assert.Equal("Event 2", result.Items[0].Title);
    }

    [Fact]
    public async Task GetAllEventsAsync_WithToFilter_ReturnsFilteredEvents()
    {
        var futureDate1 = DateTime.UtcNow.AddDays(1);
        var futureDate2 = DateTime.UtcNow.AddDays(2);
        var filterDate = futureDate1.AddHours(3);

        await _eventService.CreateEventAsync(new CreateEvent
        {
            Title = "Event 1",
            StartAt = futureDate1,
            EndAt = futureDate1.AddHours(2),
            TotalSeats = 10,
        });

        await _eventService.CreateEventAsync(new CreateEvent
        {
            Title = "Event 2",
            StartAt = futureDate2,
            EndAt = futureDate2.AddHours(2),
            TotalSeats = 10,
        });

        var result = await _eventService.GetAllEventsAsync(to: filterDate);

        Assert.Single(result.Items);
        Assert.Equal("Event 1", result.Items[0].Title);
    }

    [Fact]
    public async Task GetAllEventsAsync_WithTitleFilter_ReturnsFilteredEvents()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);

        await _eventService.CreateEventAsync(new CreateEvent
        {
            Title = "Conference 2024",
            StartAt = futureDate,
            EndAt = futureDate.AddHours(2),
            TotalSeats = 10,
        });

        await _eventService.CreateEventAsync(new CreateEvent
        {
            Title = "Meeting Q1",
            StartAt = futureDate.AddDays(1),
            EndAt = futureDate.AddDays(1).AddHours(2),
            TotalSeats = 10,
        });

        var result = await _eventService.GetAllEventsAsync(title: "Conference");

        Assert.Single(result.Items);
        Assert.Equal("Conference 2024", result.Items[0].Title);
    }

    [Fact]
    public async Task GetAllEventsAsync_WithTitleFilter_IsCaseInsensitive()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);

        await _eventService.CreateEventAsync(new CreateEvent
        {
            Title = "Conference 2024",
            StartAt = futureDate,
            EndAt = futureDate.AddHours(2),
            TotalSeats = 10,
        });

        var result = await _eventService.GetAllEventsAsync(title: "conference");

        Assert.Single(result.Items);
        Assert.Equal("Conference 2024", result.Items[0].Title);
    }

    [Fact]
    public async Task GetAllEventsAsync_WithMultipleFilters_ReturnsFilteredEvents()
    {
        var baseDate = DateTime.UtcNow.AddDays(1);

        await _eventService.CreateEventAsync(new CreateEvent
        {
            Title = "Conference 2024",
            StartAt = baseDate,
            EndAt = baseDate.AddHours(2),
            TotalSeats = 10,
        });

        await _eventService.CreateEventAsync(new CreateEvent
        {
            Title = "Conference 2025",
            StartAt = baseDate.AddDays(5),
            EndAt = baseDate.AddDays(5).AddHours(2),
            TotalSeats = 10,
        });

        var result = await _eventService.GetAllEventsAsync(
            from: baseDate.AddDays(2),
            to: baseDate.AddDays(6),
            title: "Conference");

        Assert.Single(result.Items);
        Assert.Equal("Conference 2025", result.Items[0].Title);
    }

    #endregion

    #region Pagination Tests

    [Fact]
    public async Task GetAllEventsAsync_WithDefaultPagination_ReturnsFirstPageWithDefaultPageSize()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        for (int i = 1; i <= 15; i++)
        {
            await _eventService.CreateEventAsync(new CreateEvent
            {
                Title = $"Event {i}",
                StartAt = futureDate.AddHours(i),
                EndAt = futureDate.AddHours(i + 1),
                TotalSeats = 10,
            });
        }

        var result = await _eventService.GetAllEventsAsync();

        Assert.Equal(15, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(10, result.Items.Length);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetAllEventsAsync_WithCustomPageSize_ReturnsCorrectNumberOfItems()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        for (int i = 1; i <= 25; i++)
        {
            await _eventService.CreateEventAsync(new CreateEvent
            {
                Title = $"Event {i}",
                StartAt = futureDate.AddHours(i),
                EndAt = futureDate.AddHours(i + 1),
                TotalSeats = 10,
            });
        }

        var result = await _eventService.GetAllEventsAsync(page: 1, pageSize: 5);

        Assert.Equal(25, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(5, result.Items.Length);
        Assert.Equal(5, result.TotalPages);
    }

    [Fact]
    public async Task GetAllEventsAsync_WithSecondPage_ReturnsCorrectItems()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        for (int i = 1; i <= 25; i++)
        {
            await _eventService.CreateEventAsync(new CreateEvent
            {
                Title = $"Event {i}",
                StartAt = futureDate.AddHours(i),
                EndAt = futureDate.AddHours(i + 1),
                TotalSeats = 10,
            });
        }

        var result = await _eventService.GetAllEventsAsync(page: 2, pageSize: 10);

        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(10, result.Items.Length);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task GetAllEventsAsync_WithLastPagePartialResults_ReturnsRemainingItems()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        for (int i = 1; i <= 23; i++)
        {
            await _eventService.CreateEventAsync(new CreateEvent
            {
                Title = $"Event {i}",
                StartAt = futureDate.AddHours(i),
                EndAt = futureDate.AddHours(i + 1),
                TotalSeats = 10,
            });
        }

        var result = await _eventService.GetAllEventsAsync(page: 3, pageSize: 10);

        Assert.Equal(23, result.TotalCount);
        Assert.Equal(3, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(3, result.Items.Length);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task GetAllEventsAsync_WithPageBeyondTotal_ReturnsEmptyItems()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        for (int i = 1; i <= 5; i++)
        {
            await _eventService.CreateEventAsync(new CreateEvent
            {
                Title = $"Event {i}",
                StartAt = futureDate.AddHours(i),
                EndAt = futureDate.AddHours(i + 1),
                TotalSeats = 10,
            });
        }

        var result = await _eventService.GetAllEventsAsync(page: 10, pageSize: 10);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(10, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Empty(result.Items);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task GetAllEventsAsync_WithPaginationAndFilters_ReturnsPaginatedFilteredResults()
    {
        var baseDate = DateTime.UtcNow.AddDays(1);
        for (int i = 1; i <= 30; i++)
        {
            await _eventService.CreateEventAsync(new CreateEvent
            {
                Title = $"Conference {i}",
                StartAt = baseDate.AddDays(i),
                EndAt = baseDate.AddDays(i).AddHours(2),
                TotalSeats = 10,
            });
        }

        var result = await _eventService.GetAllEventsAsync(
            page: 2,
            pageSize: 5,
            title: "Conference");

        Assert.Equal(30, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(5, result.Items.Length);
        Assert.Equal(6, result.TotalPages);
    }

    [Fact]
    public async Task GetAllEventsAsync_WithPaginationPageSizeOne_ReturnsOneItemPerPage()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        for (int i = 1; i <= 3; i++)
        {
            await _eventService.CreateEventAsync(new CreateEvent
            {
                Title = $"Event {i}",
                StartAt = futureDate.AddHours(i),
                EndAt = futureDate.AddHours(i + 1),
                TotalSeats = 10,
            });
        }

        var result = await _eventService.GetAllEventsAsync(page: 2, pageSize: 1);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.Single(result.Items);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task GetAllEventsAsync_TotalPagesCalculation_IsCorrect()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        for (int i = 1; i <= 37; i++)
        {
            await _eventService.CreateEventAsync(new CreateEvent
            {
                Title = $"Event {i}",
                StartAt = futureDate.AddHours(i),
                EndAt = futureDate.AddHours(i + 1),
                TotalSeats = 10,
            });
        }

        var result = await _eventService.GetAllEventsAsync(pageSize: 10);

        Assert.Equal(4, result.TotalPages);
    }

    [Fact]
    public async Task GetAllEventsAsync_FirstPageIsOne_NotZero()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        await _eventService.CreateEventAsync(new CreateEvent
        {
            Title = "Event 1",
            StartAt = futureDate,
            EndAt = futureDate.AddHours(1),
            TotalSeats = 10,
        });

        var result = await _eventService.GetAllEventsAsync(page: 1);

        Assert.Equal(1, result.Page);
    }

    #endregion

    #region UpdateEventAsync Tests

    [Fact]
    public async Task UpdateEventAsync_WithValidData_UpdatesEvent()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        var createdEvent = await _eventService.CreateEventAsync(new CreateEvent
        {
            Title = "Original Event",
            Description = "Original Description",
            StartAt = futureDate,
            EndAt = futureDate.AddHours(2),
            TotalSeats = 10,
        });

        var newFutureDate = DateTime.UtcNow.AddDays(2);
        var updateEvent = new UpdateEvent
        {
            Title = "Updated Event",
            Description = "Updated Description",
            StartAt = newFutureDate,
            EndAt = newFutureDate.AddHours(3)
        };

        var result = await _eventService.UpdateEventAsync(createdEvent.Id, updateEvent);

        Assert.Equal(createdEvent.Id, result.Id);
        Assert.Equal("Updated Event", result.Title);
        Assert.Equal("Updated Description", result.Description);
        Assert.Equal(newFutureDate, result.StartAt);
        Assert.Equal(newFutureDate.AddHours(3), result.EndAt);
    }

    [Fact]
    public async Task UpdateEventAsync_WithInvalidId_ThrowsNotFoundException()
    {
        var invalidId = Guid.NewGuid();
        var futureDate = DateTime.UtcNow.AddDays(1);
        var updateEvent = new UpdateEvent
        {
            Title = "Updated Event",
            StartAt = futureDate,
            EndAt = futureDate.AddHours(2)
        };

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _eventService.UpdateEventAsync(invalidId, updateEvent));
        Assert.Equal("Event not found", exception.Message);
    }

    [Fact]
    public async Task UpdateEventAsync_WithNullTitle_ThrowsValidationException()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        var createdEvent = await _eventService.CreateEventAsync(new CreateEvent
        {
            Title = "Original Event",
            StartAt = futureDate,
            EndAt = futureDate.AddHours(2),
            TotalSeats = 10,
        });

        var updateEvent = new UpdateEvent
        {
            Title = null,
            StartAt = futureDate,
            EndAt = futureDate.AddHours(2)
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _eventService.UpdateEventAsync(createdEvent.Id, updateEvent));
        Assert.Contains("Title", exception.Errors.Keys);
    }

    [Fact]
    public async Task UpdateEventAsync_WithPastStartAt_ThrowsValidationException()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        var createdEvent = await _eventService.CreateEventAsync(new CreateEvent
        {
            Title = "Original Event",
            StartAt = futureDate,
            EndAt = futureDate.AddHours(2),
            TotalSeats = 10,
        });

        var pastDate = DateTime.UtcNow.AddDays(-1);
        var updateEvent = new UpdateEvent
        {
            Title = "Updated Event",
            StartAt = pastDate,
            EndAt = pastDate.AddHours(2)
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _eventService.UpdateEventAsync(createdEvent.Id, updateEvent));
        Assert.Contains("StartAt", exception.Errors.Keys);
    }

    [Fact]
    public async Task UpdateEventAsync_WithEndAtBeforeStartAt_ThrowsValidationException()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        var createdEvent = await _eventService.CreateEventAsync(new CreateEvent
        {
            Title = "Original Event",
            StartAt = futureDate,
            EndAt = futureDate.AddHours(2),
            TotalSeats = 10,
        });

        var updateEvent = new UpdateEvent
        {
            Title = "Updated Event",
            StartAt = futureDate,
            EndAt = futureDate.AddHours(-1)
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _eventService.UpdateEventAsync(createdEvent.Id, updateEvent));
        Assert.Contains("EndAt", exception.Errors.Keys);
    }

    #endregion

    #region DeleteEventAsync Tests

    [Fact]
    public async Task DeleteEventAsync_WithValidId_ReturnsTrue()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        var createdEvent = await _eventService.CreateEventAsync(new CreateEvent
        {
            Title = "Event to Delete",
            StartAt = futureDate,
            EndAt = futureDate.AddHours(2),
            TotalSeats = 10,
        });

        var result = await _eventService.DeleteEventAsync(createdEvent.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteEventAsync_WithInvalidId_ReturnsFalse()
    {
        var invalidId = Guid.NewGuid();

        var result = await _eventService.DeleteEventAsync(invalidId);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteEventAsync_DeletedEventCannotBeRetrieved()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        var createdEvent = await _eventService.CreateEventAsync(new CreateEvent
        {
            Title = "Event to Delete",
            StartAt = futureDate,
            EndAt = futureDate.AddHours(2),
            TotalSeats = 10,
        });

        await _eventService.DeleteEventAsync(createdEvent.Id);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _eventService.GetEventByIdAsync(createdEvent.Id));
    }

    #endregion
}
