using EventTrackerApi.Domain.Models;
using EventTrackerApi.Infrastructure.DataAccess;
using EventTrackerApi.Infrastructure.DataAccess.Repositories;
using EventTrackerApi.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace EventTrackerApi.IntegrationTests;

public class RepositoryIntegrationTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.GetConnectionString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.Migrate();
        return context;
    }

    #region EventRepository

    [Fact]
    public async Task EventRepository_AddAsync_And_GetByIdAsync_ShouldReturnEvent()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new EventRepository(context);
        var @event = new Event("Test Event", "Description", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10);

        // Act
        await repository.AddAsync(@event);
        await repository.SaveChangesAsync();
        var result = await repository.GetByIdAsync(@event.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(@event.Id, result.Id);
        Assert.Equal("Test Event", result.Title);
    }

    [Fact]
    public async Task EventRepository_GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new EventRepository(context);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task EventRepository_GetAllAsync_WithoutFilters_ShouldReturnAllEvents()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new EventRepository(context);
        await repository.AddAsync(new Event("Event A", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10));
        await repository.AddAsync(new Event("Event B", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 20));
        await repository.SaveChangesAsync();

        // Act
        var result = await repository.GetEventsAsync(null, null, null, 1, 10);

        // Assert
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task EventRepository_GetAllAsync_WithTitleFilter_ShouldReturnMatchingEvents()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new EventRepository(context);
        await repository.AddAsync(new Event("Team Meeting", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10));
        await repository.AddAsync(new Event("Lunch", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10));
        await repository.SaveChangesAsync();

        // Act
        var result = await repository.GetEventsAsync("Meeting", null, null, 1, 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Team Meeting", result.Items.First().Title);
    }

    [Fact]
    public async Task EventRepository_GetAllAsync_WithDateRangeFilter_ShouldReturnEventsInRange()
    {
        // Arrange
        var baseDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        using var context = CreateContext();
        var repository = new EventRepository(context);
        await repository.AddAsync(new Event("Before", null, baseDate.AddDays(-5), baseDate.AddDays(-5).AddHours(1), 10));
        await repository.AddAsync(new Event("In Range", null, baseDate.AddDays(5), baseDate.AddDays(5).AddHours(1), 10));
        await repository.AddAsync(new Event("After", null, baseDate.AddDays(15), baseDate.AddDays(15).AddHours(1), 10));
        await repository.SaveChangesAsync();

        // Act
        var result = await repository.GetEventsAsync(null, baseDate, baseDate.AddDays(10), 1, 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("In Range", result.Items.First().Title);
    }

    [Fact]
    public async Task EventRepository_GetAllAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new EventRepository(context);
        for (int i = 1; i <= 15; i++)
        {
            await repository.AddAsync(new Event($"Event {i:00}", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10));
        }
        await repository.SaveChangesAsync();

        // Act
        var result = await repository.GetEventsAsync(null, null, null, 2, 5);

        // Assert
        Assert.Equal(5, result.Items.Count());
    }

    [Fact]
    public async Task EventRepository_Update_ShouldPersistChanges()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new EventRepository(context);
        var @event = new Event("Original", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10);
        await repository.AddAsync(@event);
        await repository.SaveChangesAsync();

        // Act
        var updatedEvent = new Event(@event.Id, "Updated", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 10, 10);
        repository.SetValues(@event, updatedEvent);
        await repository.SaveChangesAsync();
        var result = await repository.GetByIdAsync(@event.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated", result.Title);
    }

    [Fact]
    public async Task EventRepository_Delete_ShouldRemoveEvent()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new EventRepository(context);
        var @event = new Event("To Delete", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10);
        await repository.AddAsync(@event);
        await repository.SaveChangesAsync();

        // Act
        repository.Remove(@event);
        await repository.SaveChangesAsync();
        var result = await repository.GetByIdAsync(@event.Id);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region BookingRepository

    [Fact]
    public async Task BookingRepository_AddAsync_And_GetByIdAsync_ShouldReturnBookingWithEvent()
    {
        // Arrange
        using var context = CreateContext();
        var eventRepository = new EventRepository(context);
        var bookingRepository = new BookingRepository(context);

        var @event = new Event("Test Event", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10);
        await eventRepository.AddAsync(@event);
        await eventRepository.SaveChangesAsync();

        var booking = new Booking(@event.Id, Guid.NewGuid());
        await bookingRepository.AddAsync(booking);
        await bookingRepository.SaveChangesAsync();

        // Act
        var result = await bookingRepository.GetByIdAsync(booking.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
        Assert.NotNull(result.Event);
        Assert.Equal(@event.Id, result.Event.Id);
    }

    [Fact]
    public async Task BookingRepository_GetPendingBookingsAsync_ShouldReturnOnlyPending()
    {
        // Arrange
        using var context = CreateContext();
        var eventRepository = new EventRepository(context);
        var bookingRepository = new BookingRepository(context);

        var @event = new Event("Test Event", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10);
        await eventRepository.AddAsync(@event);
        await eventRepository.SaveChangesAsync();

        var userId = Guid.NewGuid();
        var pending = new Booking(@event.Id, userId);
        var confirmed = new Booking(@event.Id, userId);
        confirmed.Confirm();
        var rejected = new Booking(@event.Id, userId);
        rejected.Reject();

        await bookingRepository.AddAsync(pending);
        await bookingRepository.AddAsync(confirmed);
        await bookingRepository.AddAsync(rejected);
        await bookingRepository.SaveChangesAsync();

        // Act
        var result = await bookingRepository.GetPendingAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(BookingStatus.Pending, result.First().Status);
    }

    #endregion

    #region Migrations

    [Fact]
    public async Task Migrations_ShouldCreateEventsAndBookingsTables()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        await using var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var eventsCmd = connection.CreateCommand();
        eventsCmd.CommandText = "SELECT 1 FROM information_schema.tables WHERE table_name = 'events'";
        var eventsResult = await eventsCmd.ExecuteScalarAsync();

        await using var bookingsCmd = connection.CreateCommand();
        bookingsCmd.CommandText = "SELECT 1 FROM information_schema.tables WHERE table_name = 'bookings'";
        var bookingsResult = await bookingsCmd.ExecuteScalarAsync();

        // Assert
        Assert.NotNull(eventsResult);
        Assert.NotNull(bookingsResult);
    }

    [Fact]
    public async Task Migrations_ShouldCreateForeignKeyConstraint()
    {
        // Arrange
        using var context = CreateContext();
        var eventRepository = new EventRepository(context);
        var bookingRepository = new BookingRepository(context);

        var @event = new Event("Test", null, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10);
        await eventRepository.AddAsync(@event);
        await eventRepository.SaveChangesAsync();

        var booking = new Booking(@event.Id, Guid.NewGuid());
        await bookingRepository.AddAsync(booking);
        await bookingRepository.SaveChangesAsync();

        // Act & Assert — если FK нарушен, будет DbUpdateException
        var result = await bookingRepository.GetByIdAsync(booking.Id);
        Assert.NotNull(result);
        Assert.Equal(@event.Id, result.EventId);
    }

    [Fact]
    public async Task Migrations_ForeignKeyViolation_ShouldThrowException()
    {
        // Arrange
        using var context = CreateContext();
        var bookingRepository = new BookingRepository(context);
        var userRepository = new UserRepository(context);
        var user = new User("testuser", "hash", UserRole.User);
        await userRepository.AddAsync(user);
        await userRepository.SaveChangesAsync();

        var booking = new Booking(Guid.NewGuid(), user.Id);
        await bookingRepository.AddAsync(booking);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => bookingRepository.SaveChangesAsync());
    }

    #endregion
}
