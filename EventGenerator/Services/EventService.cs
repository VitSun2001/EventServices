using Domain.Entities;
using EventGenerator.Configurations;
using Microsoft.Extensions.Options;
using Shared.Requests;

namespace EventGenerator.Services;

public class EventService : IEventService
{
    private readonly EventServiceOptions _options;
    private readonly HttpClient _httpClient;
    private readonly Random _random = new();

    public EventService(IOptions<EventServiceOptions> options, HttpClient httpClient)
    {
        _options = options.Value;
        _httpClient = httpClient;
    }

    public Event GenerateEvent()
    {
        Guid.NewGuid();
        var enumValues = Enum.GetValues<EventTypeEnum>();
        var randomTypeIndex = _random.Next(0, enumValues.Length);
        var type = enumValues.ElementAt(_random.Next(0, randomTypeIndex));
        return new Event(type);
    }

    public Task<HttpResponseMessage> SendEvent(Event @event) => _httpClient.PostAsJsonAsync(
        _options.EventProcessorEndpoint,
        new SendEventRequest(@event.Id, @event.Type, @event.Time)
    );
}