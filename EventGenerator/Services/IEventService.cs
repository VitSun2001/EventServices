using Domain.Entities;

namespace EventGenerator.Services;

public interface IEventService
{
    Event GenerateEvent();
    Task<HttpResponseMessage> SendEvent(Event @event);
}