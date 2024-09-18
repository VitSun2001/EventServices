using System.Linq.Expressions;
using Domain.Entities;
using EventProcessor.Configurations;
using EventProcessor.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Queries;
using Shared.Requests;
using Shared.Responses;

namespace EventProcessor.Services;

public class IncidentsService : IIncidentsService
{
    private static readonly Dictionary<SendEventRequest, DateTime> SecondTypeEventsQueue = new();
    private static readonly object Lock = new();

    private readonly EventProcessorOptions _options;
    private readonly EventProcessorDbContext _dbContext;

    public IncidentsService(IOptions<EventProcessorOptions> options, EventProcessorDbContext dbContext)
    {
        _options = options.Value;
        _dbContext = dbContext;
    }

    public async Task<GetIncidentsResponse> GetIncidents(GetIncidentsQuery query)
    {
        throw new NotImplementedException();
    }

    public async Task HandleEventRequest(SendEventRequest request)
    {
        var requestDateTime = DateTime.UtcNow;

        switch (request.Type)
        {
            case EventTypeEnum.First:
            {
                await HandleFirstEventType(request, requestDateTime);
                break;
            }
            case EventTypeEnum.Second:
            {
                HandleSecondEventType(request, requestDateTime);
                break;
            }
            case EventTypeEnum.Third:
            case EventTypeEnum.Fourth:
            default:
                return;
        }
    }

    private async Task HandleFirstEventType(SendEventRequest request, DateTime requestDateTime)
    {
        IncidentTypeEnum incidentType;
        var eventsToAdd = new Dictionary<SendEventRequest, DateTime>();

        lock (Lock)
        {
            if (
                SecondTypeEventsQueue.Count == 0 ||
                request.Time - SecondTypeEventsQueue.MaxBy(x => x.Value).Value >
                TimeSpan.FromMilliseconds(_options.IncidentGracePeriodMillis)
            )
            {
                incidentType = IncidentTypeEnum.First;
            }
            else
            {
                incidentType = IncidentTypeEnum.Second;
                foreach (var (key, value) in eventsToAdd)
                {
                    eventsToAdd.Add(key, value);
                }

                SecondTypeEventsQueue.Clear();
            }
        }

        var incident = new Incident(incidentType, requestDateTime);

        eventsToAdd.Add(request, requestDateTime);
        foreach (var eventToAdd in eventsToAdd.Select(requestToAdd =>
                     new Event(requestToAdd.Key.Type) {Id = requestToAdd.Key.Id, Time = requestToAdd.Key.Time,}
                 ))
        {
            await _dbContext.Events.AddAsync(eventToAdd);
            incident.Events.Add(eventToAdd);
        }

        await _dbContext.AddAsync(incident);
        await _dbContext.SaveChangesAsync();
    }

    private static void HandleSecondEventType(SendEventRequest request, DateTime requestDateTime)
    {
        lock (Lock)
        {
            SecondTypeEventsQueue.Add(request, requestDateTime);
        }
    }
}