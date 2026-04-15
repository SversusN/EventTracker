using System.ComponentModel.DataAnnotations;

namespace EventTrackerApi.Models;

public class Event
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime StartAt { get; private set; }
    public DateTime EndAt { get; private set; }
    public int TotalSeats { get; private set; }
    public int AvailableSeats { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="titile">Наименование</param>
    /// <param name="description">Описание</param>
    /// <param name="startAt">Дата начала</param>
    /// <param name="endAt">Дата окончания</param>
    /// <param name="totalSeats">Общее количество мест</param>
    public Event(string titile, string? description, DateTime startAt, DateTime endAt, int totalSeats)
    {
        if (totalSeats <= 0)
        {
            throw new ValidationException("TotalSeats must be greater than 0.");
        }

        Id = Guid.NewGuid();
        Title = titile;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
        TotalSeats = totalSeats;
        AvailableSeats = totalSeats;
    }

    //Нужен для FromUpdateDto
    public Event(Guid id, string title, string? description, DateTime startAt, DateTime endAt, int totalSeats, int availableSeats)
    {
        Id = id;
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
        TotalSeats = totalSeats;
        AvailableSeats = availableSeats;
    }

    /// <summary>
    /// Пытается зарезервировать указанное количество мест
    /// </summary>
    /// <param name="count">Количество мест для резервирования</param>
    /// <returns>true, если места есть и успешно зарезервированы; иначе false</returns>
    public bool TryReserveSeats(int count = 1)
    {
        if (AvailableSeats < count)
        {
            return false;
        }

        AvailableSeats -= count;
        return true;
    }

    /// <summary>
    /// Освобождает указанное количество мест
    /// </summary>
    /// <param name="count">Количество мест для освобождения</param>
    public void ReleaseSeats(int count = 1)
    {
        AvailableSeats += count;
        if (AvailableSeats > TotalSeats)
        {
            AvailableSeats = TotalSeats;
        }
    }
}
