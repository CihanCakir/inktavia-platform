using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Message;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.Message;

/// <summary>
/// Anti-harassment gate: a provider cannot free-text a customer until the customer has replied (channelOpen).
/// The offer itself is delivered as a MessageType.Offer (exempt from the gate, created on submit).
/// </summary>
[DocumentationInfo("Send message command handler", "Creates a service request message with anti-harassment gate.")]
public sealed class SendServiceRequestMessageCommandHandler : AizenCommandHandler<SendServiceRequestMessageCommand, SendServiceRequestMessageResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestMessageRepository _messageRepository;
    private readonly IServiceRequestOfferRepository _offerRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;
    private readonly IAizenMessagePublisher _messagePublisher;

    public SendServiceRequestMessageCommandHandler(
        IServiceRequestRepository srRepository,
        IServiceRequestMessageRepository messageRepository,
        IServiceRequestOfferRepository offerRepository,
        IAizenInfoAccessor info,
        ServiceRequestRealtimePublisher realtimePublisher,
        IAizenMessagePublisher messagePublisher)
    {
        _srRepository = srRepository;
        _messageRepository = messageRepository;
        _offerRepository = offerRepository;
        _info = info;
        _realtimePublisher = realtimePublisher;
        _messagePublisher = messagePublisher;
    }

    public override async Task<SendServiceRequestMessageResponse?> Handle(SendServiceRequestMessageCommand request, CancellationToken cancellationToken)
    {
        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new AizenBusinessException("Service request not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var req = request.Request;

        // Anti-harassment gate: provider free text only after customer has replied
        if (request.SenderType == ServiceRequestMessageSenderType.Provider)
        {
            var channelOpen = await _messageRepository.HasOwnerMessageAsync(sr.Id, cancellationToken);
            if (!channelOpen)
                throw new AizenBusinessException("SR_MSG_CHANNEL_LOCKED");
        }

        ServiceRequestMessageEntity message;
        if (req.LocationLat.HasValue && req.LocationLng.HasValue)
        {
            message = ServiceRequestMessageEntity.CreateLocation(
                sr.Id, currentUserId, request.SenderType,
                req.LocationLat.Value, req.LocationLng.Value, req.LocationLabel);
        }
        else
        {
            var msgType = req.AttachmentFileId.HasValue
                ? ServiceRequestMessageType.Image
                : ServiceRequestMessageType.Text;
            message = ServiceRequestMessageEntity.Create(
                sr.Id, currentUserId, request.SenderType,
                msgType, req.Content, req.AttachmentFileId);
        }

        await _messageRepository.AddAsync(message, cancellationToken);

        // Resolve provider profile for realtime — notify the counterparty, not self
        long? providerProfileId = null;
        if (request.SenderType == ServiceRequestMessageSenderType.Owner)
        {
            var offers = await _offerRepository.GetByServiceRequestIdAsync(sr.Id, cancellationToken);
            providerProfileId = offers.FirstOrDefault(o => !o.IsDeleted)?.ProviderProfileId;
        }

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, providerProfileId,
            ServiceRequestRealtimeEventType.MessageSent, message.ToDto(),
            currentUserId, request.SenderType == ServiceRequestMessageSenderType.Owner
                ? ServiceRequestActorType.Owner : ServiceRequestActorType.Provider, cancellationToken);

        await _messagePublisher.PublishAsync(new ServiceRequestMessageSentMessage
        {
            ServiceRequestId = sr.Id,
            MessageId = message.Id,
            SenderUserId = currentUserId,
            SenderType = request.SenderType,
            ProviderProfileId = providerProfileId
        }, cancellationToken);

        return new SendServiceRequestMessageResponse(message.ToDto());
    }
}
