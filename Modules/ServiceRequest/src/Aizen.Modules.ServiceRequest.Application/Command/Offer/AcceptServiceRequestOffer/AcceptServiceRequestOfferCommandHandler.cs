using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.Offer;

[DocumentationInfo("Accept offer command handler", "Marks the offer as accepted, updates service request to OfferAccepted, publishes realtime event.")]
public sealed class AcceptServiceRequestOfferCommandHandler : AizenCommandHandler<AcceptServiceRequestOfferCommand, AcceptServiceRequestOfferResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestOfferRepository _offerRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;

    public AcceptServiceRequestOfferCommandHandler(
        IServiceRequestRepository srRepository, IServiceRequestOfferRepository offerRepository,
        IAizenInfoAccessor info, ServiceRequestRealtimePublisher realtimePublisher)
    {
        _srRepository = srRepository; _offerRepository = offerRepository;
        _info = info; _realtimePublisher = realtimePublisher;
    }

    public override async Task<AcceptServiceRequestOfferResponse?> Handle(AcceptServiceRequestOfferCommand request, CancellationToken cancellationToken)
    {
        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");
        var offer = await _offerRepository.GetByIdAsync(request.Request.OfferId, cancellationToken)
            ?? throw new InvalidOperationException($"Offer {request.Request.OfferId} not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var prevStatus = sr.Status;

        offer.Accept();
        _offerRepository.Update(offer);

        sr.ChangeStatus(ServiceRequestStatus.OfferAccepted);
        var history = ServiceRequestStatusHistoryEntity.Create(
            sr.Id, prevStatus, ServiceRequestStatus.OfferAccepted,
            "Offer accepted", currentUserId, ServiceRequestActorType.Owner);
        sr.AddStatusHistory(history);
        _srRepository.Update(sr);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, offer.ProviderProfileId,
            ServiceRequestRealtimeEventType.OfferAccepted, offer.ToDto(),
            currentUserId, ServiceRequestActorType.Owner, cancellationToken);

        return new AcceptServiceRequestOfferResponse(offer.Id, sr.Id);
    }
}
