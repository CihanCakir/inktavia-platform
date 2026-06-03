using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.Dispute;

[DocumentationInfo("Resolve dispute command handler", "Admin resolves the dispute, transitions SR to DisputeResolved, publishes DisputeResolved.")]
public sealed class ResolveServiceRequestDisputeCommandHandler : AizenCommandHandler<ResolveServiceRequestDisputeCommand, ResolveServiceRequestDisputeResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestDisputeRepository _disputeRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;

    public ResolveServiceRequestDisputeCommandHandler(
        IServiceRequestRepository srRepository, IServiceRequestDisputeRepository disputeRepository,
        IAizenInfoAccessor info, ServiceRequestRealtimePublisher realtimePublisher)
    {
        _srRepository = srRepository; _disputeRepository = disputeRepository;
        _info = info; _realtimePublisher = realtimePublisher;
    }

    public override async Task<ResolveServiceRequestDisputeResponse?> Handle(ResolveServiceRequestDisputeCommand request, CancellationToken cancellationToken)
    {
        var dispute = await _disputeRepository.GetByIdAsync(request.DisputeId, cancellationToken)
            ?? throw new InvalidOperationException($"Dispute {request.DisputeId} not found.");
        var sr = await _srRepository.GetByIdAsync(dispute.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {dispute.ServiceRequestId} not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        dispute.Resolve(currentUserId, request.Request.ResolutionNotes);
        _disputeRepository.Update(dispute);

        var prevStatus = sr.Status;
        sr.ChangeStatus(ServiceRequestStatus.DisputeResolved);
        var history = ServiceRequestStatusHistoryEntity.Create(
            sr.Id, prevStatus, ServiceRequestStatus.DisputeResolved,
            "Dispute resolved", currentUserId, ServiceRequestActorType.Admin);
        sr.AddStatusHistory(history);
        _srRepository.Update(sr);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, null,
            ServiceRequestRealtimeEventType.DisputeResolved, dispute.ToDto(),
            currentUserId, ServiceRequestActorType.Admin, cancellationToken);

        return new ResolveServiceRequestDisputeResponse(dispute.Id);
    }
}
