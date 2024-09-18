using Domain.Entities;

namespace Shared.Requests;

public record SendEventRequest(Guid Id, EventTypeEnum Type, DateTime Time);