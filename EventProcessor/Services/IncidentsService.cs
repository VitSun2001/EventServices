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
    private static readonly Queue<Tuple<SendEventRequest, DateTime>> EventsOfSecondTypeQueue = new();
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
        Expression<Func<Incident, object>> keySelector = query.SortColumn?.ToLower() switch
        {
            "type" => incident => incident.Type,
            _ => incident => incident.Time
        };

        var incidentsQuery = query.OrderBy == "desc"
            ? _dbContext.Incidents.OrderByDescending(keySelector)
            : _dbContext.Incidents.OrderBy(keySelector);

        var incidents = await incidentsQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(x => x.Events.OrderByDescending(e => e.Time))
            .Select(x => new GetIncidentsResponse.Incident(x.Id, x.Type, x.Time,
                x.Events.Select(e => new GetIncidentsResponse.Event(e.Id, e.Type, e.Time))))
            .AsNoTracking()
            .ToListAsync();

        var count = _dbContext.Incidents.Count();

        return new GetIncidentsResponse(
            query.Page,
            incidents.Count,
            count,
            query.SortColumn,
            query.OrderBy,
            incidents
        );
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
        var eventRequestsToAdd = new List<SendEventRequest>();

        SendEventRequest? eventOfSecondType = null;
        lock (Lock)
        {
            while (EventsOfSecondTypeQueue.Count != 0)
            {
                var dequeue = EventsOfSecondTypeQueue.Dequeue();
                if (requestDateTime - dequeue.Item2 < TimeSpan.FromMilliseconds(_options.IncidentGracePeriodMillis))
                {
                    eventOfSecondType = dequeue.Item1;
                    break;
                }
            }
        }

        if (eventOfSecondType == null)
        {
            incidentType = IncidentTypeEnum.First;
        }
        else
        {
            incidentType = IncidentTypeEnum.Second;
            eventRequestsToAdd.Add(eventOfSecondType);
        }
        eventRequestsToAdd.Add(request);
        
        var incident = new Incident(incidentType, requestDateTime);

        var eventsToAdd = eventRequestsToAdd.Select(x => new Event(x.Type)
        {
            Id = x.Id,
            Time = x.Time
        });
        
        foreach (var @event in eventsToAdd)
        {
           await _dbContext.Events.AddAsync(@event);
           incident.Events.Add(@event);
        }
        
        await _dbContext.AddAsync(incident);
        await _dbContext.SaveChangesAsync();
    }

    private static void HandleSecondEventType(SendEventRequest request, DateTime requestDateTime)
    {
        lock (Lock)
        {
            EventsOfSecondTypeQueue.Enqueue(Tuple.Create(request, requestDateTime));
        }
    }
}