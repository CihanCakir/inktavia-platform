using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.CargoDry.Abstraction.RemoteCall;
using Aizen.Modules.ServiceRequest.Abstraction.Constants;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.ServiceRequest.Application.Command.Offer.CreateCargoDrySupplyOffer;

[DocumentationInfo("Create CargoDry supply offer command handler",
    "Provider 'accept' of a CARGODRY_SUPPLY request: server-pins a single non-editable offer at the product's fixed " +
    "retail price. Gated server-side on an ACTIVE ConsignmentAgreement (program membership). No bidding.")]
public sealed class CreateCargoDrySupplyOfferCommandHandler
    : AizenCommandHandler<CreateCargoDrySupplyOfferCommand, CreateCargoDrySupplyOfferResponse>
{
    private readonly IServiceRequestRepository       _srRepository;
    private readonly IServiceRequestOfferRepository  _offerRepository;
    private readonly IAizenInfoAccessor              _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;
    private readonly IAizenMessagePublisher          _messagePublisher;
    private readonly ServiceRequestDbContext         _db;
    private readonly ICargoDrySupplyRemoteCall       _cargoDry;
    private readonly ILogger<CreateCargoDrySupplyOfferCommandHandler> _logger;

    public CreateCargoDrySupplyOfferCommandHandler(
        IServiceRequestRepository       srRepository,
        IServiceRequestOfferRepository  offerRepository,
        IAizenInfoAccessor              info,
        ServiceRequestRealtimePublisher realtimePublisher,
        IAizenMessagePublisher          messagePublisher,
        ServiceRequestDbContext         db,
        ICargoDrySupplyRemoteCall       cargoDry,
        ILogger<CreateCargoDrySupplyOfferCommandHandler> logger)
    {
        _srRepository      = srRepository;
        _offerRepository   = offerRepository;
        _info              = info;
        _realtimePublisher = realtimePublisher;
        _messagePublisher  = messagePublisher;
        _db                = db;
        _cargoDry          = cargoDry;
        _logger            = logger;
    }

    public override async Task<CreateCargoDrySupplyOfferResponse?> Handle(
        CreateCargoDrySupplyOfferCommand request, CancellationToken cancellationToken)
    {
        var sr = await _srRepository.GetByIdWithDetailsAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        if (!string.Equals(sr.ServiceCategoryCode, ServiceRequestServiceCategoryCodes.CargoDrySupply, StringComparison.OrdinalIgnoreCase))
            throw new AizenBusinessException("This is not a CargoDry supply request.");
        if (string.IsNullOrWhiteSpace(sr.CargoDryProductCode))
            throw new AizenBusinessException("This CargoDry supply request has no product code.");
        if (sr.Status is not (ServiceRequestStatus.Open or ServiceRequestStatus.WaitingForOffer or ServiceRequestStatus.OfferReceived))
            throw new AizenBusinessException("This request is not open for acceptance.");

        // Provider identity from the trusted token context — never the body (mirrors CreateServiceRequestOffer).
        var providerProfileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (providerProfileId <= 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var rawToken      = _info.UserInfoAccessor.UserInfo.AccessToken;

        // One offer per provider per request.
        var existing = sr.Offers.FirstOrDefault(o =>
            o.ProviderProfileId == providerProfileId && !o.IsDeleted &&
            o.Status is ServiceRequestOfferStatus.Draft or ServiceRequestOfferStatus.Submitted or ServiceRequestOfferStatus.Accepted);
        if (existing is not null)
            throw new AizenBusinessException("You have already accepted this CargoDry supply request.");

        // ── Server-side gate + price pin: ACTIVE ConsignmentAgreement required; retail price is authoritative. ──
        var ctx = await _cargoDry.GetSupplyAcceptContextAsync(
            providerProfileId, sr.CargoDryProductCode!, $"Bearer {rawToken}", cancellationToken);
        if (!ctx.ProductActive)
            throw new AizenBusinessException("SR_CARGODRY_PRODUCT_UNAVAILABLE");
        if (!ctx.ProviderHasActiveAgreement)
            throw new AizenBusinessException("SR_CARGODRY_NOT_PROGRAM_PROVIDER");

        var currency = string.IsNullOrWhiteSpace(ctx.CurrencyCode) ? "TRY" : ctx.CurrencyCode!;

        // ── Build the pinned offer (single Product line at retail; no editable amount, no bidding). ──
        var offer = ServiceRequestOfferEntity.Create(
            serviceRequestId:         sr.Id,
            providerProfileId:        providerProfileId,
            providerUserId:           currentUserId,
            totalAmount:              ctx.RetailPrice,
            currencyCode:             currency,
            description:              $"CargoDry supply — {sr.CargoDryProductCode} (fixed retail price)",
            providerNotes:            null,
            estimatedStartDate:       null,
            estimatedEndDate:         null,
            estimatedDurationMinutes: null,
            expiresAt:                null);
        offer.AddItem(ServiceRequestOfferItemEntity.Create(
            serviceRequestOfferId: 0,
            itemType:              ServiceRequestOfferItemType.Product,
            title:                 $"CargoDry {sr.CargoDryProductCode}",
            description:           "CargoDry supply kit (fixed retail price)",
            quantity:              1,
            unitPrice:             ctx.RetailPrice,
            currencyCode:          currency,
            sortOrder:             0));
        offer.Submit();
        await _offerRepository.AddAsync(offer, cancellationToken);

        if (sr.Status is ServiceRequestStatus.Open or ServiceRequestStatus.WaitingForOffer)
        {
            var prevStatus = sr.Status;
            sr.ChangeStatus(ServiceRequestStatus.OfferReceived);
            sr.AddStatusHistory(ServiceRequestStatusHistoryEntity.Create(
                sr.Id, prevStatus, ServiceRequestStatus.OfferReceived,
                "CargoDry supply accepted (pinned retail offer)", currentUserId, ServiceRequestActorType.Provider));
            _srRepository.Update(sr);
        }

        // Flush so the offer id is assigned before publishing (mirrors CreateServiceRequestOffer).
        await _db.SaveChangesAsync(cancellationToken);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, providerProfileId,
            ServiceRequestRealtimeEventType.OfferCreated, offer.ToDto(),
            currentUserId, ServiceRequestActorType.Provider, cancellationToken);

        await _messagePublisher.PublishAsync(new ServiceRequestOfferCreatedMessage
        {
            ServiceRequestId  = sr.Id,
            OfferId           = offer.Id,
            ProviderProfileId = providerProfileId,
            ProviderUserId    = currentUserId,
            OwnerUserId       = sr.OwnerUserId,
            TotalAmount       = offer.GrandTotal,
            CurrencyCode      = currency,
            Status            = offer.Status,
        }, cancellationToken);

        _logger.LogInformation(
            "CargoDry supply offer pinned. SR={SrId} Offer={OfferId} Provider={Provider} Retail={Retail} {Currency}",
            sr.Id, offer.Id, providerProfileId, ctx.RetailPrice, currency);

        return new CreateCargoDrySupplyOfferResponse
        {
            OfferId          = offer.Id,
            ServiceRequestId = sr.Id,
            RetailPrice      = ctx.RetailPrice,
            CurrencyCode     = currency,
        };
    }
}
