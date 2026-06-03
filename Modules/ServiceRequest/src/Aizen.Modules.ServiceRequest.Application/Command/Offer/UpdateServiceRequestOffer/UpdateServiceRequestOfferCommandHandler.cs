using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.Offer;

[DocumentationInfo("Update offer command handler", "Provider updates an existing offer and publishes OfferUpdated realtime event.")]
public sealed class UpdateServiceRequestOfferCommandHandler : AizenCommandHandler<UpdateServiceRequestOfferCommand, UpdateServiceRequestOfferResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestOfferRepository _offerRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;

    public UpdateServiceRequestOfferCommandHandler(
        IServiceRequestRepository srRepository,
        IServiceRequestOfferRepository offerRepository,
        IAizenInfoAccessor info,
        ServiceRequestRealtimePublisher realtimePublisher)
    {
        _srRepository = srRepository;
        _offerRepository = offerRepository;
        _info = info;
        _realtimePublisher = realtimePublisher;
    }

    public override async Task<UpdateServiceRequestOfferResponse?> Handle(UpdateServiceRequestOfferCommand request, CancellationToken cancellationToken)
    {
        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        var offer = await _offerRepository.GetByIdAsync(request.OfferId, cancellationToken)
            ?? throw new InvalidOperationException($"Offer {request.OfferId} not found.");

        var req = request.Request;
        offer.Update(
            offer.TotalAmount,
            req.CurrencyCode,
            req.Description,
            req.ProviderNotes,
            req.EstimatedStartDate,
            req.EstimatedEndDate,
            req.EstimatedDurationMinutes,
            req.ExpiresAt);

        _offerRepository.Update(offer);

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, offer.ProviderProfileId,
            ServiceRequestRealtimeEventType.OfferUpdated, offer.ToDto(),
            currentUserId, ServiceRequestActorType.Provider, cancellationToken);

        return new UpdateServiceRequestOfferResponse(offer.ToDto());
    }
}
