namespace Domain.Entities;

public class Event
{
    public Guid Id { get; set; }
    public EventTypeEnum Type { get; set; }
    public DateTime Time { get; set; }
    
    public Event(EventTypeEnum type)
    {
        Id = Guid.NewGuid();
        Type = type;
        Time = DateTime.UtcNow;
    }

    private Event()
    {
    }
}

public enum EventTypeEnum
{
    First = 1,
    Second,
    Third,
    Fourth
}