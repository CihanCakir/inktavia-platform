using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Application.Mapping;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Application.Configuration;
using Aizen.Modules.ServiceRequest.Repository.Mapping;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;

[DocumentationInfo("Cancel service request command handler", "Cancels the service request and records status history.")]
public sealed class CancelServiceRequestCommandHandler : AizenCommandHandler<CancelServiceRequestCommand, CancelServiceRequestResponse>
{
    private readonly IServiceRequestRepository _repository;
    private readonly IServiceRequestMessageRepository _msgRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;
    private readonly IAizenMessagePublisher _messagePublisher;
    private readonly IOptionsMonitor<MessagingWriteCutoverOptions> _cutover;

    public CancelServiceRequestCommandHandler(IServiceRequestRepository repository, IServiceRequestMessageRepository msgRepository, IAizenInfoAccessor info, ServiceRequestRealtimePublisher realtimePublisher, IAizenMessagePublisher messagePublisher, IOptionsMonitor<MessagingWriteCutoverOptions> cutover)
    {
        _repository = repository; _msgRepository = msgRepository; _info = info; _realtimePublisher = realtimePublisher; _messagePublisher = messagePublisher; _cutover = cutover;
    }

    public override async Task<CancelServiceRequestResponse?> Handle(CancelServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var previousStatus = entity.Status;
        entity.Cancel(currentUserId, request.Request.Reason, request.Request.ReasonCode);

        // N-E — map the structured cancel reason to a Payment RefundReason so the refund flow allocates deterministically.
        var refundReason = ServiceRequestReasonRefundMap.ToRefundReason(request.Request.ReasonCode);

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
            RefundReasonCode = (int)refundReason,
            CancellationReason = request.Request.Reason,
        }, cancellationToken);

        // Lifecycle system message (idempotent)
        // BE_WC1 flag-gated: Messaging generates CONVERSATION_CLOSED from ServiceRequestCancelledMessage when ON.
        if (!_cutover.CurrentValue.SystemMessages &&
            !await _msgRepository.HasSystemMessageAsync(entity.Id, "CONVERSATION_CLOSED", cancellationToken))
        {
            var sysMsg = ServiceRequestMessageEntity.Create(
                entity.Id, currentUserId, ServiceRequestMessageSenderType.System,
                ServiceRequestMessageType.StatusChange, "CONVERSATION_CLOSED", null);
            await _msgRepository.AddAsync(sysMsg, cancellationToken);

            // Phase-2 live-sync: mirror the system message into the Messaging store (provider id resolved by the sync).
            await _messagePublisher.PublishAsync(new ServiceRequestMessageSentMessage
            {
                ServiceRequestId = entity.Id,
                MessageId = sysMsg.Id,
                SenderUserId = currentUserId,
                SenderType = ServiceRequestMessageSenderType.System,
                ProviderProfileId = null,
                Content = sysMsg.Content,
                MessageType = sysMsg.MessageType,
                OccurredAt = DateTimeOffset.UtcNow,
            }, cancellationToken);
        }

        return new CancelServiceRequestResponse(entity.Id);
    }
}
