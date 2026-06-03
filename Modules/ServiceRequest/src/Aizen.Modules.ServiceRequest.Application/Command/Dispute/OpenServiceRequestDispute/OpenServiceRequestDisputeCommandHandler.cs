using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Dispute;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.Dispute;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.Dispute;

[DocumentationInfo("Open dispute command handler", "Creates dispute entity, transitions SR to DisputeOpened, publishes DisputeOpened and AdminInterventionRequired.")]
public sealed class OpenServiceRequestDisputeCommandHandler : AizenCommandHandler<OpenServiceRequestDisputeCommand, OpenServiceRequestDisputeResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestDisputeRepository _disputeRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;

    public OpenServiceRequestDisputeCommandHandler(
        IServiceRequestRepository srRepository, IServiceRequestDisputeRepository disputeRepository,
        IAizenInfoAccessor info, ServiceRequestRealtimePublisher realtimePublisher)
    {
        _srRepository = srRepository; _disputeRepository = disputeRepository;
        _info = info; _realtimePublisher = realtimePublisher;
    }

    public override async Task<OpenServiceRequestDisputeResponse?> Handle(OpenServiceRequestDisputeCommand request, CancellationToken cancellationToken)
    {
        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var req = request.Request;

        var dispute = ServiceRequestDisputeEntity.Create(
            sr.Id, currentUserId, request.ActorType, req.Reason, req.Description);

        await _disputeRepository.AddAsync(dispute, cancellationToken);

        var prevStatus = sr.Status;
        sr.ChangeStatus(ServiceRequestStatus.DisputeOpened);
        sr.SetDispute(dispute);
        var history = ServiceRequestStatusHistoryEntity.Create(
            sr.Id, prevStatus, ServiceRequestStatus.DisputeOpened,
            "Dispute opened", currentUserId, request.ActorType);
        sr.AddStatusHistory(history);
        _srRepository.Update(sr);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, null,
            ServiceRequestRealtimeEventType.DisputeOpened, dispute.ToDto(),
            currentUserId, request.ActorType, cancellationToken);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, null,
            ServiceRequestRealtimeEventType.AdminInterventionRequired, dispute.ToDto(),
            currentUserId, request.ActorType, cancellationToken);

        return new OpenServiceRequestDisputeResponse(dispute.ToDto());
    }
}
