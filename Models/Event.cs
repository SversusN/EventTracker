namespace EventTrackerApi.Models;

public class Event
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime StartAt { get; private set; }
    public DateTime EndAt { get; private set; }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="titile">Наименование</param>
    /// <param name="description">Описание</param>
    /// <param name="startAt">Дата начала</param>
    /// <param name="endAt">Дата окончания</param>
    public Event(string titile, string? description, DateTime startAt, DateTime endAt)
    {
        Id = Guid.NewGuid();
        Title = titile;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
    }
    public void Update(string title, string? description, DateTime startAt, DateTime endAt)
    {
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
    }
}



