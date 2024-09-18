namespace Domain.Entities;

public class Incident
{
    public Guid Id { get; set; }
    public IncidentTypeEnum Type { get; set; }
    public DateTime Time { get; set; }
    public ICollection<Event> Events { get; } = new List<Event>();
    
    public Incident(IncidentTypeEnum type, DateTime time)
    {
        Id = Guid.NewGuid();
        Type = type;
        Time = time;
    }
    
    private Incident()
    {
    }
}

public enum IncidentTypeEnum
{
    First = 1,
    Second,
}