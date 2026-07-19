using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
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
    private readonly IServiceRequestMessageRepository _msgRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;
    private readonly IAizenMessagePublisher _messagePublisher;

    public CancelServiceRequestCommandHandler(IServiceRequestRepository repository, IServiceRequestMessageRepository msgRepository, IAizenInfoAccessor info, ServiceRequestRealtimePublisher realtimePublisher, IAizenMessagePublisher messagePublisher)
    {
        _repository = repository; _msgRepository = msgRepository; _info = info; _realtimePublisher = realtimePublisher; _messagePublisher = messagePublisher;
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

        await _messagePublisher.PublishAsync(new ServiceRequestCancelledMessage
        {
            ServiceRequestId = entity.Id,
            RequestCode = entity.RequestCode,
            LocationCityCode = entity.LocationCityCode,
            CancelledByUserId = currentUserId,
        }, cancellationToken);

        // Lifecycle system message (idempotent)
        if (!await _msgRepository.HasSystemMessageAsync(entity.Id, "CONVERSATION_CLOSED", cancellationToken))
        {
            var sysMsg = ServiceRequestMessageEntity.Create(
                entity.Id, currentUserId, ServiceRequestMessageSenderType.System,
                ServiceRequestMessageType.StatusChange, "CONVERSATION_CLOSED", null);
            await _msgRepository.AddAsync(sysMsg, cancellationToken);
        }

        return new CancelServiceRequestResponse(entity.Id);
    }
}
