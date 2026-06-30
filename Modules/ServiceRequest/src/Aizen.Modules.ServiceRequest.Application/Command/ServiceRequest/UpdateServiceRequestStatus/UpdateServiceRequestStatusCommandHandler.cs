using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;

[DocumentationInfo("Update service request status command handler", "Changes the status and appends a status history entry.")]
public sealed class UpdateServiceRequestStatusCommandHandler : AizenCommandHandler<UpdateServiceRequestStatusCommand, UpdateServiceRequestStatusResponse>
{
    private readonly IServiceRequestRepository _repository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;

    public UpdateServiceRequestStatusCommandHandler(
        IServiceRequestRepository repository, IAizenInfoAccessor info,
        ServiceRequestRealtimePublisher realtimePublisher)
    {
        _repository = repository; _info = info; _realtimePublisher = realtimePublisher;
    }

    public override async Task<UpdateServiceRequestStatusResponse?> Handle(UpdateServiceRequestStatusCommand request, CancellationToken cancellationToken)
    {
        var sr = await _repository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var prevStatus = sr.Status;
        var newStatus = request.Request.Status;

        var history = ServiceRequestStatusHistoryEntity.Create(
            sr.Id, prevStatus, newStatus, request.Request.Reason, currentUserId, ServiceRequestActorType.Admin);
        sr.AddStatusHistory(history);
        sr.ChangeStatus(newStatus);
        _repository.Update(sr);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, sr.Assignment?.ProviderProfileId,
            ServiceRequestRealtimeEventType.ServiceRequestStatusChanged, new { ServiceRequestId = sr.Id, OldStatus = prevStatus, NewStatus = newStatus },
            currentUserId, ServiceRequestActorType.Admin, cancellationToken);

        return new UpdateServiceRequestStatusResponse(sr.Id, newStatus.ToString());
    }
}
