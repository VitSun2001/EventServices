namespace EventProcessor.Configurations;

public class EventProcessorOptions
{
    public int IncidentGracePeriodMillis { get; set; }
    public bool UseAlternativeIncidentPipeline { get; set; }
    public string AlternativeIncidentHttpListenerUri { get; set; }
}