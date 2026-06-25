using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Message;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.Message;

[DocumentationInfo("Send message command handler", "Creates and persists a service request message, publishes MessageSent realtime event.")]
public sealed class SendServiceRequestMessageCommandHandler : AizenCommandHandler<SendServiceRequestMessageCommand, SendServiceRequestMessageResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestMessageRepository _messageRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;

    public SendServiceRequestMessageCommandHandler(
        IServiceRequestRepository srRepository, IServiceRequestMessageRepository messageRepository,
        IAizenInfoAccessor info, ServiceRequestRealtimePublisher realtimePublisher)
    {
        _srRepository = srRepository; _messageRepository = messageRepository;
        _info = info; _realtimePublisher = realtimePublisher;
    }

    public override async Task<SendServiceRequestMessageResponse?> Handle(SendServiceRequestMessageCommand request, CancellationToken cancellationToken)
    {
        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var req = request.Request;

        var message = ServiceRequestMessageEntity.Create(
            sr.Id, currentUserId, request.SenderType,
            ServiceRequestMessageType.Text, req.Content, req.AttachmentFileId);

        await _messageRepository.AddAsync(message, cancellationToken);
        sr.AddMessage(message);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, null,
            ServiceRequestRealtimeEventType.MessageSent, message.ToDto(),
            currentUserId, request.SenderType == ServiceRequestMessageSenderType.Owner
                ? ServiceRequestActorType.Owner : ServiceRequestActorType.Provider, cancellationToken);

        return new SendServiceRequestMessageResponse(message.ToDto());
    }
}
