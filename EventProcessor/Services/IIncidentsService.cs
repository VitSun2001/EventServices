using Shared.Queries;
using Shared.Requests;
using Shared.Responses;

namespace EventProcessor.Services;

public interface IIncidentsService
{
    Task<GetIncidentsResponse> GetIncidents(GetIncidentsQuery query);
    Task HandleEventRequest(SendEventRequest request);
}