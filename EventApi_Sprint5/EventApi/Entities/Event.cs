using EventApi.Exceptions;

namespace EventApi.Entities;

internal sealed class Event
{
    internal Guid Id { get; private set; }
    internal string Title { get; private set; }
    internal string? Description { get; private set; }
    internal DateTime StartAt { get; private set; }
    internal DateTime EndAt { get; private set; }
    internal int TotalSeats { get; private set; }
    internal int AvailableSeats { get; private set; }
    internal ICollection<Booking> Bookings { get; private set; } = [];

    private Event() { Title = null!; }

    private Event(
        Guid id,
        string title,
        DateTime startAt,
        DateTime endAt,
        int totalSeats,
        string? description = null,
        string? location = null)
    {
        Id = id;
        Title = title;
        StartAt = startAt;
        EndAt = endAt;
        TotalSeats = totalSeats;
        AvailableSeats = totalSeats;
        Description = description;
    }

    internal static Event Create(
        string? title,
        DateTime? startAt,
        DateTime? endAt,
        int? totalSeats = null,
        string? description = null,
        string? location = null)
    {
        ThrowIfNotValid(title, startAt, endAt, totalSeats);

        return new Event(Guid.NewGuid(), title!.Trim(), startAt!.Value, endAt!.Value, totalSeats!.Value, description, location);
    }

    internal void Update(
       string? title,
       DateTime? startAt,
       DateTime? endAt,
       string? description = null,
       string? location = null)
    {
        ThrowIfNotValid(title, startAt, endAt, TotalSeats);

        Title = title!;
        StartAt = startAt!.Value;
        EndAt = endAt!.Value;
        Description = description;
    }

    internal bool TryReserveSeats(int count = 1)
    {
        if (AvailableSeats < count)
            return false;

        AvailableSeats -= count;
        return true;
    }

    internal void ReleaseSeats(int count = 1)
    {
        AvailableSeats = Math.Min(TotalSeats, AvailableSeats + count);
    }

    private static void ThrowIfNotValid(
        string? title,
        DateTime? startAt,
        DateTime? endAt,
        int? totalSeats)
    {
        var errors = new Dictionary<string, ICollection<string>>();

        if (string.IsNullOrWhiteSpace(title))
            AddError(errors, nameof(Title), "Title cannot be empty");

        if (!startAt.HasValue)
            AddError(errors, nameof(StartAt), "Start time cannot be null");

        if (!endAt.HasValue)
            AddError(errors, nameof(EndAt), "End time cannot be null");

        if (startAt < DateTime.UtcNow)
            AddError(errors, nameof(StartAt), "Event cannot start in the past");

        if (endAt <= startAt)
            AddError(errors, nameof(EndAt), "End time must be after start time");

        if (!totalSeats.HasValue || totalSeats.Value <= 0)
            AddError(errors, nameof(TotalSeats), "TotalSeats must be greater than zero");

        if (errors.Any())
            throw new ValidationException(errors);
    }

    private static void AddError(Dictionary<string, ICollection<string>> errors, string field, string message)
    {
        if (!errors.ContainsKey(field))
            errors[field] = new List<string>();

        errors[field].Add(message);
    }
}
