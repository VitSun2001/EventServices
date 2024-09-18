using Domain.Entities;

namespace Shared.Responses;

public record GetIncidentsResponse(
    int Page,
    int Count,
    int TotalCount,
    string? SortColumn,
    string? OrderBy,
    List<GetIncidentsResponse.Incident> Data)
{
    public record Incident(Guid Id, IncidentTypeEnum Type, DateTime Time, IEnumerable<Event> Events);

    public record Event(Guid Id, EventTypeEnum Type, DateTime Time);
}