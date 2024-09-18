using EventGenerator.Configurations;
using EventGenerator.Services;
using Microsoft.Extensions.Options;

namespace EventGenerator.Workers;

public sealed class EventGeneratorWorker : BackgroundService
{
    private readonly EventGeneratorOptions _options;
    private readonly IEventService _eventService;
    private readonly Random _random = new();

    public EventGeneratorWorker(IOptions<EventGeneratorOptions> config, IEventService eventService)
    {
        _options = config.Value;
        _eventService = eventService;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var generatedEvent = _eventService.GenerateEvent();
            _eventService.SendEvent(generatedEvent);

            var delay = TimeSpan.FromMilliseconds(
                _random.Next(_options.MinDelayBetweenEventsMillis, _options.MaxDelayBetweenEventsMillis + 1)
            );
            await Task.Delay(delay, cancellationToken);
        }
    }
}