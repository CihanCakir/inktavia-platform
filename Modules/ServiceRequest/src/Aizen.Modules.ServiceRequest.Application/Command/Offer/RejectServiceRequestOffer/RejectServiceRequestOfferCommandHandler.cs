using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.Offer;

[DocumentationInfo("Reject offer command handler", "Rejects the offer and publishes OfferRejected realtime event.")]
public sealed class RejectServiceRequestOfferCommandHandler : AizenCommandHandler<RejectServiceRequestOfferCommand, RejectServiceRequestOfferResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestOfferRepository _offerRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;
    private readonly IAizenMessagePublisher _messagePublisher;

    public RejectServiceRequestOfferCommandHandler(
        IServiceRequestRepository srRepository, IServiceRequestOfferRepository offerRepository,
        IAizenInfoAccessor info, ServiceRequestRealtimePublisher realtimePublisher,
        IAizenMessagePublisher messagePublisher)
    {
        _srRepository = srRepository; _offerRepository = offerRepository;
        _info = info; _realtimePublisher = realtimePublisher;
        _messagePublisher = messagePublisher;
    }

    public override async Task<RejectServiceRequestOfferResponse?> Handle(RejectServiceRequestOfferCommand request, CancellationToken cancellationToken)
    {
        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");
        var offer = await _offerRepository.GetByIdAsync(request.Request.OfferId, cancellationToken)
            ?? throw new InvalidOperationException($"Offer {request.Request.OfferId} not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        offer.Reject(request.Request.Reason, request.Request.ReasonCode);
        _offerRepository.Update(offer);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, offer.ProviderProfileId,
            ServiceRequestRealtimeEventType.OfferRejected, offer.ToDto(),
            currentUserId, ServiceRequestActorType.Owner, cancellationToken);

        await _messagePublisher.PublishAsync(new ServiceRequestOfferRejectedMessage
        {
            ServiceRequestId = sr.Id,
            OfferId = offer.Id,
            OwnerUserId = currentUserId,
            ProviderProfileId = offer.ProviderProfileId,
            Reason = request.Request.Reason,
            ReasonCode = request.Request.ReasonCode
        }, cancellationToken);

        return new RejectServiceRequestOfferResponse(offer.Id);
    }
}
