using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;

[DocumentationInfo("Dispute service request command handler", "Opens a dispute, updates status to DisputeOpened, publishes event.")]
public sealed class DisputeServiceRequestCommandHandler : AizenCommandHandler<DisputeServiceRequestCommand, DisputeServiceRequestResponse>
{
    private readonly IServiceRequestRepository _repository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;

    public DisputeServiceRequestCommandHandler(
        IServiceRequestRepository repository, IAizenInfoAccessor info,
        ServiceRequestRealtimePublisher realtimePublisher)
    {
        _repository = repository; _info = info; _realtimePublisher = realtimePublisher;
    }

    public override async Task<DisputeServiceRequestResponse?> Handle(DisputeServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var sr = await _repository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var prevStatus = sr.Status;

        sr.MarkDisputed(request.Request.Reason, DateTimeOffset.UtcNow);
        var history = ServiceRequestStatusHistoryEntity.Create(
            sr.Id, prevStatus, ServiceRequestStatus.DisputeOpened,
            request.Request.Reason, currentUserId, ServiceRequestActorType.Admin);
        sr.AddStatusHistory(history);
        _repository.Update(sr);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, sr.Assignment?.ProviderProfileId,
            ServiceRequestRealtimeEventType.DisputeOpened,
            new { ServiceRequestId = sr.Id, Reason = request.Request.Reason },
            currentUserId, ServiceRequestActorType.Admin, cancellationToken);

        return new DisputeServiceRequestResponse(sr.Id);
    }
}
