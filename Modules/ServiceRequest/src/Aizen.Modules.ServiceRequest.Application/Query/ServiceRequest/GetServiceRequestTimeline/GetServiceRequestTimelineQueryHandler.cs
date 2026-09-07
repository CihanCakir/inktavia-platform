using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Query.ServiceRequest;

[DocumentationInfo("Get service request timeline query handler", "Maps status history to timeline events.")]
public sealed class GetServiceRequestTimelineQueryHandler : AizenQueryHandler<GetServiceRequestTimelineQuery, GetServiceRequestTimelineResponse>
{
    private readonly IServiceRequestRepository _repository;

    public GetServiceRequestTimelineQueryHandler(IServiceRequestRepository repository) => _repository = repository;

    public override async Task<GetServiceRequestTimelineResponse> Handle(GetServiceRequestTimelineQuery request, CancellationToken cancellationToken)
    {
        var sr = await _repository.GetByIdWithDetailsAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        var events = sr.StatusHistory
            .OrderBy(x => x.OccurredAt)
            .Select((h, i) => new ServiceRequestTimelineEventItemDto
            {
                Id = (i + 1).ToString(),
                Event = h.ToStatus.ToString(),
                EventCode = Abstraction.Timeline.ServiceRequestTimelineEventCode.Derive(h.FromStatus, h.ToStatus),
                Description = h.Reason,
                By = h.ActorUserId?.ToString(),
                Status = h.ToStatus.ToString().ToLowerInvariant(),
                At = new DateTimeOffset(h.OccurredAt, TimeSpan.Zero)
            })
            .ToList();

        return new GetServiceRequestTimelineResponse(sr.Id.ToString(), events);
    }
}
