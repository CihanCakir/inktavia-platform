
namespace Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

[DocumentationInfo("Get service request timeline response", "Timeline events for a specific service request.")]
public sealed class GetServiceRequestTimelineResponse(string requestId, List<ServiceRequestTimelineEventItemDto> events)
{
    public string RequestId { get; } = requestId;
    public List<ServiceRequestTimelineEventItemDto> Events { get; } = events;
}

public sealed record ServiceRequestTimelineEventItemDto
{
    public string Id { get; init; } = string.Empty;
    public string Event { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? By { get; init; }
    public string? Status { get; init; }
    public DateTimeOffset At { get; init; }
}
