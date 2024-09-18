using System.Diagnostics;
using System.Linq.Expressions;
using Domain.Entities;
using EventProcessor.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Queries;
using Shared.Requests;
using Shared.Responses;

namespace EventProcessor.Services;

public class IncidentsServiceSingleton : IIncidentsService
{
    private readonly Dictionary<SendEventRequest, DateTime> _eventsOfSecondTypeToAdd = new();
    private readonly Stopwatch _stopwatch = new();
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
        IncidentTypeEnum incidentType;
        var eventsToAdd = new Dictionary<SendEventRequest, DateTime>();

        if (_eventsOfSecondTypeToAdd.Count == 0 || _stopwatch.Elapsed > TimeSpan.FromSeconds(20))
        {
            incidentType = IncidentTypeEnum.First;
        }
        else
        {
            incidentType = IncidentTypeEnum.Second;
            _stopwatch.Restart();

            foreach (var (key, value) in eventsToAdd)
            {
                eventsToAdd.Add(key, value);
            }

            _eventsOfSecondTypeToAdd.Clear();
        }

        var incident = new Incident(incidentType, requestDateTime);

        using var scope = _serviceScopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<EventProcessorDbContext>();

        eventsToAdd.Add(request, requestDateTime);
        foreach (var eventToAdd in eventsToAdd.Select(requestToAdd =>
                     new Event(requestToAdd.Key.Type) {Id = requestToAdd.Key.Id, Time = requestToAdd.Key.Time}
                 ))
        {
            await dbContext.Events.AddAsync(eventToAdd);
            incident.Events.Add(eventToAdd);
        }

        await dbContext.AddAsync(incident);
        await dbContext.SaveChangesAsync();
    }

    private void HandleSecondEventType(SendEventRequest request, DateTime requestDateTime)
    {
        if (_stopwatch.IsRunning)
            _stopwatch.Restart();
        else
            _stopwatch.Start();

        _eventsOfSecondTypeToAdd.Add(request, requestDateTime);
    }
}