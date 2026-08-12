using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;
using Aizen.Modules.ServiceRequest.Repository.Persistence;

namespace Aizen.Modules.ServiceRequest.Application.Command.Offer;

[DocumentationInfo("Create offer command handler", "Creates a provider offer with line items and publishes OfferCreated realtime event.")]
public sealed class CreateServiceRequestOfferCommandHandler : AizenCommandHandler<CreateServiceRequestOfferCommand, CreateServiceRequestOfferResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestOfferRepository _offerRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;
    private readonly IAizenMessagePublisher _messagePublisher;
    private readonly ServiceRequestDbContext _db;

    public CreateServiceRequestOfferCommandHandler(
        IServiceRequestRepository srRepository,
        IServiceRequestOfferRepository offerRepository,
        IAizenInfoAccessor info,
        ServiceRequestRealtimePublisher realtimePublisher,
        IAizenMessagePublisher messagePublisher,
        ServiceRequestDbContext db)
    {
        _srRepository = srRepository; _offerRepository = offerRepository;
        _info = info; _realtimePublisher = realtimePublisher;
        _messagePublisher = messagePublisher; _db = db;
    }

    public override async Task<CreateServiceRequestOfferResponse?> Handle(CreateServiceRequestOfferCommand request, CancellationToken cancellationToken)
    {
        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;

        // The provider identity comes from the trusted request context (the BFF asserts it via
        // X-Aizen-Provider-Profile-Id), never from the request body. Taking `ProviderProfileId` off the payload —
        // as this handler used to — lets a caller file an offer under ANOTHER provider's profile: the offer then
        // shows up in that provider's list (the queries filter by ProfileId) while update/withdraw still check
        // ProviderUserId, so the two identities on one row disagree. This is the same hole that was just closed
        // on update/withdraw; it was still open on create.
        var providerProfileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (providerProfileId <= 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var req = request.Request;
        var total = req.Items.Sum(i => i.Quantity * i.UnitPrice);

        var offer = ServiceRequestOfferEntity.Create(
            sr.Id, providerProfileId, currentUserId,
            total, req.CurrencyCode, req.Description, req.ProviderNotes,
            req.EstimatedStartDate, req.EstimatedEndDate, req.EstimatedDurationMinutes, req.ExpiresAt);

        // Build the aggregate FULLY, then add it once.
        //
        // The previous version added the offer, then attached its items and called Update() on it. EF had not
        // assigned a real key yet, so marking the same instance Modified threw:
        //   "The property 'ServiceRequestOfferEntity.Id' has a temporary value while attempting to change the
        //    entity's state to 'Modified'."
        // Every offer submission died with a 500. The item's OfferId is set by the relationship fix-up on save —
        // it must not be read off the parent before the insert.
        int sortOrder = 0;
        foreach (var item in req.Items)
        {
            var offerItem = ServiceRequestOfferItemEntity.Create(
                0, item.ItemType, item.Title, item.Description,
                item.Quantity, item.UnitPrice, item.CurrencyCode, sortOrder++,
                item.UnitCode, item.TaxRate, item.DiscountType, item.DiscountValue);
            offer.AddItem(offerItem);
        }

        offer.Submit();

        await _offerRepository.AddAsync(offer, cancellationToken);

        if (sr.Status == ServiceRequestStatus.Open || sr.Status == ServiceRequestStatus.WaitingForOffer)
        {
            sr.ChangeStatus(ServiceRequestStatus.OfferReceived);
            _srRepository.Update(sr);
        }

        // BE_WC1b — flush so the DB assigns the offer identity BEFORE we publish. Previously the OfferCreated realtime
        // event, the ServiceRequestOfferCreatedMessage, and the BFF response all carried offer.Id = 0 (identity is
        // assigned on save, not on AddAsync) — which made the Messaging OFFER card key on sys:{srId}:OFFER:0 and
        // collide across offers. (Same intra-handler flush pattern as SaveOfferDraft.)
        await _db.SaveChangesAsync(cancellationToken);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, providerProfileId,
            ServiceRequestRealtimeEventType.OfferCreated, offer.ToDto(),
            currentUserId, ServiceRequestActorType.Provider, cancellationToken);

        await _messagePublisher.PublishAsync(new ServiceRequestOfferCreatedMessage
        {
            ServiceRequestId = sr.Id,
            OfferId = offer.Id,
            ProviderProfileId = providerProfileId,
            ProviderUserId = currentUserId,
            OwnerUserId = sr.OwnerUserId, // BE_NF1 (D2) — carry the owner so Notification can notify them.
            TotalAmount = total,
            CurrencyCode = req.CurrencyCode,
            Status = offer.Status // BE_WC1b — Submitted here (offer.Submit() above) → drives the Messaging OFFER card
        }, cancellationToken);

        return new CreateServiceRequestOfferResponse(offer.ToDto());
    }
}
