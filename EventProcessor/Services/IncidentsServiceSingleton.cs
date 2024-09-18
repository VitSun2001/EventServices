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

public class IncidentsServiceSingleton : IIncidentsService
{
    private readonly Queue<Tuple<SendEventRequest, DateTime>> _eventsOfSecondTypeQueue = new();
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public IncidentsServiceSingleton(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }
    
    public async Task<GetIncidentsResponse> GetIncidents(GetIncidentsQuery query)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventProcessorDbContext>();

        Expression<Func<Incident, object>> keySelector = query.SortColumn?.ToLower() switch
        {
            "type" => incident => incident.Type,
            _ => incident => incident.Time
        };

        var incidentsQuery = query.OrderBy == "desc"
            ? dbContext.Incidents.OrderByDescending(keySelector)
            : dbContext.Incidents.OrderBy(keySelector);
        
        var incidents = await incidentsQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(x => x.Events.OrderByDescending(e => e.Time))
            .Select(x => new GetIncidentsResponse.Incident(x.Id, x.Type, x.Time,
                x.Events.Select(e => new GetIncidentsResponse.Event(e.Id, e.Type, e.Time))))
            .AsNoTracking()
            .ToListAsync();
        
        var count = dbContext.Incidents.Count();

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
                break;
        }
    }

    private async Task HandleFirstEventType(SendEventRequest request, DateTime requestDateTime)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<EventProcessorOptions>>().Value;
        
        IncidentTypeEnum incidentType;
        var eventRequestsToAdd = new List<SendEventRequest>();

        SendEventRequest? eventOfSecondType = null;
        while (_eventsOfSecondTypeQueue.Count != 0)
        {
            var dequeue = _eventsOfSecondTypeQueue.Dequeue();
            if (requestDateTime - dequeue.Item2 >=
                TimeSpan.FromMilliseconds(options.IncidentGracePeriodMillis)) continue;
            eventOfSecondType = dequeue.Item1;
            break;
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
        
        var dbContext = scope.ServiceProvider.GetRequiredService<EventProcessorDbContext>();
        
        var eventsToAdd = eventRequestsToAdd.Select(x => new Event(x.Type)
        {
            Id = x.Id,
            Time = x.Time
        });
        
        foreach (var @event in eventsToAdd)
        {
            await dbContext.Events.AddAsync(@event);
            incident.Events.Add(@event);
        }
        
        await dbContext.AddAsync(incident);
        await dbContext.SaveChangesAsync();
    }

    private void HandleSecondEventType(SendEventRequest request, DateTime requestDateTime)
    {
        _eventsOfSecondTypeQueue.Enqueue(Tuple.Create(request, requestDateTime));
    }
}