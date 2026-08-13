using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Application.Services;
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
    private readonly OfferCalculationService _calculation;
    private readonly UnitCodeValidator _unitCodeValidator;
    private readonly Services.Fx.OfferFxResolver _fxResolver;

    public CreateServiceRequestOfferCommandHandler(
        IServiceRequestRepository srRepository,
        IServiceRequestOfferRepository offerRepository,
        IAizenInfoAccessor info,
        ServiceRequestRealtimePublisher realtimePublisher,
        IAizenMessagePublisher messagePublisher,
        ServiceRequestDbContext db,
        OfferCalculationService calculation,
        UnitCodeValidator unitCodeValidator,
        Services.Fx.OfferFxResolver fxResolver)
    {
        _srRepository = srRepository; _offerRepository = offerRepository;
        _info = info; _realtimePublisher = realtimePublisher;
        _messagePublisher = messagePublisher; _db = db;
        _calculation = calculation; _unitCodeValidator = unitCodeValidator; _fxResolver = fxResolver;
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

        // FIX_OFFER_LINE_PRICING_ON_CREATE — this one-shot create+submit path previously persisted the offer with
        // UNPRICED lines (LineSubtotal/TaxAmount/CommissionBaseAmount = 0), so the accept-time §19.2 economics saw a ₺0
        // service and rejected every accept with "provider −2.14". Run the SAME server-authoritative pricing sequence
        // SubmitOffer runs, BEFORE persisting, so a created offer is fully priced (parity with draft→submit).
        var itemsForValidation = offer.Items
            .Where(i => !i.IsDeleted && !string.IsNullOrWhiteSpace(i.UnitCode))
            .Select(i => new Abstraction.Request.Offer.CreateServiceRequestOfferItemRequest { UnitCode = i.UnitCode, Title = i.Title })
            .ToList();
        if (itemsForValidation.Count > 0)
            await _unitCodeValidator.ValidateUnitCodesAsync(itemsForValidation, cancellationToken);

        // Reject empty offers (no priced lines) — parity with SubmitOffer; prevents the degenerate ₺0 offer.
        var pricedLines = offer.Items.Where(i => i.ItemType != ServiceRequestOfferItemType.Discount && !i.IsDeleted).ToList();
        if (pricedLines.Count == 0)
            throw new AizenBusinessException("SR_OFFER_EMPTY");

        // BE-S3a — resolve FX at the create instant, converting any foreign lines to TRY BEFORE the economics runs.
        // Fail-loud (SR_FX_RATE_UNAVAILABLE) if a source currency has no effective rate. TRY-only offers resolve nothing.
        var createInstant = DateTime.UtcNow;
        await _fxResolver.ResolveAndConvertAsync(offer, createInstant, cancellationToken);

        // Server-authoritative pricing — fills every per-line LineSubtotal/TaxAmount/LineTotal/CommissionBaseAmount + the
        // offer-level Subtotal/TaxTotal/GrandTotal/CommissionBaseTotal (offer.ToDto() then reads the computed fields).
        _calculation.Calculate(offer);

        offer.Submit();

        await _offerRepository.AddAsync(offer, cancellationToken);

        if (sr.Status == ServiceRequestStatus.Open || sr.Status == ServiceRequestStatus.WaitingForOffer)
        {
            var prevStatus = sr.Status;
            sr.ChangeStatus(ServiceRequestStatus.OfferReceived);
            // QA4 — record the →OfferReceived transition so the SR timeline reflects the real lifecycle.
            sr.AddStatusHistory(ServiceRequestStatusHistoryEntity.Create(
                sr.Id, prevStatus, ServiceRequestStatus.OfferReceived, "First offer received", currentUserId, ServiceRequestActorType.Provider));
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
            TotalAmount = offer.GrandTotal, // FIX_OFFER_LINE_PRICING_ON_CREATE — computed total, not the naive Σ(qty×price).
            CurrencyCode = req.CurrencyCode,
            Status = offer.Status // BE_WC1b — Submitted here (offer.Submit() above) → drives the Messaging OFFER card
        }, cancellationToken);

        return new CreateServiceRequestOfferResponse(offer.ToDto());
    }
}
