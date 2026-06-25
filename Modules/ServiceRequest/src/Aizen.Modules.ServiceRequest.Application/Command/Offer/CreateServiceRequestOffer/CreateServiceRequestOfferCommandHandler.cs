using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.Offer;

[DocumentationInfo("Create offer command handler", "Creates a provider offer with line items and publishes OfferCreated realtime event.")]
public sealed class CreateServiceRequestOfferCommandHandler : AizenCommandHandler<CreateServiceRequestOfferCommand, CreateServiceRequestOfferResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestOfferRepository _offerRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;

    public CreateServiceRequestOfferCommandHandler(
        IServiceRequestRepository srRepository,
        IServiceRequestOfferRepository offerRepository,
        IAizenInfoAccessor info,
        ServiceRequestRealtimePublisher realtimePublisher)
    {
        _srRepository = srRepository; _offerRepository = offerRepository;
        _info = info; _realtimePublisher = realtimePublisher;
    }

    public override async Task<CreateServiceRequestOfferResponse?> Handle(CreateServiceRequestOfferCommand request, CancellationToken cancellationToken)
    {
        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var req = request.Request;
        var total = req.Items.Sum(i => i.Quantity * i.UnitPrice);

        var offer = ServiceRequestOfferEntity.Create(
            sr.Id, req.ProviderProfileId, currentUserId,
            total, req.CurrencyCode, req.Description, req.ProviderNotes,
            req.EstimatedStartDate, req.EstimatedEndDate, req.EstimatedDurationMinutes, req.ExpiresAt);

        await _offerRepository.AddAsync(offer, cancellationToken);

        int sortOrder = 0;
        foreach (var item in req.Items)
        {
            var offerItem = ServiceRequestOfferItemEntity.Create(
                offer.Id, item.ItemType, item.Title, item.Description,
                item.Quantity, item.UnitPrice, item.CurrencyCode, sortOrder++);
            offer.AddItem(offerItem);
        }

        offer.Submit();
        _offerRepository.Update(offer);

        if (sr.Status == ServiceRequestStatus.Open || sr.Status == ServiceRequestStatus.WaitingForOffer)
        {
            sr.ChangeStatus(ServiceRequestStatus.OfferReceived);
            _srRepository.Update(sr);
        }

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, req.ProviderProfileId,
            ServiceRequestRealtimeEventType.OfferCreated, offer.ToDto(),
            currentUserId, ServiceRequestActorType.Provider, cancellationToken);

        return new CreateServiceRequestOfferResponse(offer.ToDto());
    }
}
