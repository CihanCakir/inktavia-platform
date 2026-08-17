using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.Dispute;

[DocumentationInfo("Change dispute status command handler", "Admin changes dispute status and publishes DisputeStatusChanged event.")]
public sealed class ChangeServiceRequestDisputeStatusCommandHandler : AizenCommandHandler<ChangeServiceRequestDisputeStatusCommand, bool>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestDisputeRepository _disputeRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;

    public ChangeServiceRequestDisputeStatusCommandHandler(
        IServiceRequestRepository srRepository, IServiceRequestDisputeRepository disputeRepository,
        IAizenInfoAccessor info, ServiceRequestRealtimePublisher realtimePublisher)
    {
        _srRepository = srRepository; _disputeRepository = disputeRepository;
        _info = info; _realtimePublisher = realtimePublisher;
    }

    public override async Task<bool> Handle(ChangeServiceRequestDisputeStatusCommand request, CancellationToken cancellationToken)
    {
        var dispute = await _disputeRepository.GetByIdAsync(request.DisputeId, cancellationToken)
            ?? throw new InvalidOperationException($"Dispute {request.DisputeId} not found.");
        var sr = await _srRepository.GetByIdAsync(dispute.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {dispute.ServiceRequestId} not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        dispute.ChangeStatus(request.Request.Status);
        _disputeRepository.Update(dispute);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, null,
            ServiceRequestRealtimeEventType.DisputeStatusChanged, dispute.ToDto(),
            currentUserId, ServiceRequestActorType.Admin, cancellationToken);

        return true;
    }
}
