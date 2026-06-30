using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;

[DocumentationInfo("Cancel service request command handler", "Cancels the service request and records status history.")]
public sealed class CancelServiceRequestCommandHandler : AizenCommandHandler<CancelServiceRequestCommand, CancelServiceRequestResponse>
{
    private readonly IServiceRequestRepository _repository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;

    public CancelServiceRequestCommandHandler(IServiceRequestRepository repository, IAizenInfoAccessor info, ServiceRequestRealtimePublisher realtimePublisher)
    {
        _repository = repository; _info = info; _realtimePublisher = realtimePublisher;
    }

    public override async Task<CancelServiceRequestResponse?> Handle(CancelServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var previousStatus = entity.Status;
        entity.Cancel(currentUserId, request.Request.Reason);

        var history = ServiceRequestStatusHistoryEntity.Create(
            entity.Id, previousStatus, ServiceRequestStatus.Cancelled,
            request.Request.Reason, currentUserId, ServiceRequestActorType.Owner);
        entity.AddStatusHistory(history);
        _repository.Update(entity);

        await _realtimePublisher.PublishAsync(entity.Id, entity.RequestCode, entity.OwnerUserId, null,
            ServiceRequestRealtimeEventType.ServiceRequestStatusChanged,
            entity.ToDto(), currentUserId, ServiceRequestActorType.Owner, cancellationToken);

        return new CancelServiceRequestResponse(entity.Id);
    }
}
