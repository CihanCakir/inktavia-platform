using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Aizen.Modules.ServiceRequest.Application.Query.Offer.GetOfferCommissionPreview;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;
using Microsoft.Extensions.Logging;
using SrEnum = Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Application.Command.Offer;

[DocumentationInfo("Accept offer command handler", "Marks the offer as accepted, updates service request to OfferAccepted, creates payment escrow, publishes realtime event.")]
public sealed class AcceptServiceRequestOfferCommandHandler : AizenCommandHandler<AcceptServiceRequestOfferCommand, AcceptServiceRequestOfferResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestOfferRepository _offerRepository;
    private readonly IServiceRequestMessageRepository _msgRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;
    private readonly IPaymentModuleRemoteCall _paymentRemoteCall;
    private readonly IAizenMessagePublisher _messagePublisher;
    private readonly Services.Pricing.PricingAttributeSnapshotResolver _pricingSnapshotResolver;
    private readonly ILogger<AcceptServiceRequestOfferCommandHandler> _logger;

    public AcceptServiceRequestOfferCommandHandler(
        IServiceRequestRepository srRepository, IServiceRequestOfferRepository offerRepository,
        IServiceRequestMessageRepository msgRepository,
        IAizenInfoAccessor info, ServiceRequestRealtimePublisher realtimePublisher,
        IPaymentModuleRemoteCall paymentRemoteCall,
        IAizenMessagePublisher messagePublisher,
        Services.Pricing.PricingAttributeSnapshotResolver pricingSnapshotResolver,
        ILogger<AcceptServiceRequestOfferCommandHandler> logger)
    {
        _srRepository = srRepository; _offerRepository = offerRepository; _msgRepository = msgRepository;
        _info = info; _realtimePublisher = realtimePublisher;
        _paymentRemoteCall = paymentRemoteCall; _messagePublisher = messagePublisher;
        _pricingSnapshotResolver = pricingSnapshotResolver; _logger = logger;
    }

    public override async Task<AcceptServiceRequestOfferResponse?> Handle(AcceptServiceRequestOfferCommand request, CancellationToken cancellationToken)
    {
        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");
        var offer = await _offerRepository.GetByIdAsync(request.Request.OfferId, cancellationToken)
            ?? throw new InvalidOperationException($"Offer {request.Request.OfferId} not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var rawToken     = _info.UserInfoAccessor.UserInfo.AccessToken; // forwarded to Payment module
        var prevStatus   = sr.Status;

        // ── BE-P8: acceptance economics + escrow BEFORE committing acceptance ──────────────────
        // Run the Payment combiner (plan → S7 commission → P3 fee → P5 gate → S8 snapshot → escrow).
        // On Rejected/ConfigurationError we BLOCK acceptance (throw) — no half-accepted offer, no escrow.
        // The whole SR command is transactional, so throwing here rolls back everything.
        // S2d — resolve each line's pricing attribute values (+ denormalized labels) to snapshot at acceptance.
        var attributesByLineRef = await _pricingSnapshotResolver.ResolveForOfferAsync(offer, cancellationToken);
        var economicsRequest = BuildEconomicsRequest(sr, offer, attributesByLineRef);
        var economics = await _paymentRemoteCall.CalculateServiceRequestEconomicsAsync(
            economicsRequest, $"Bearer {rawToken}", cancellationToken);

        // BE-I1: a non-split-eligible provider is a distinct hard block (surface ProviderNotSplitEligible).
        if (!economics.ProviderSplitEligible)
        {
            _logger.LogWarning(
                "Offer acceptance blocked for SR {SrId} Offer {OfferId}: provider not split-eligible — {Reason}",
                sr.Id, offer.Id, economics.Reason);
            throw new AizenBusinessException(
                (int)Aizen.Modules.Payment.Abstraction.Enum.PaymentErrorCode.ProviderNotSplitEligible);
        }

        if (!economics.CanProceed)
        {
            _logger.LogWarning(
                "Offer acceptance blocked for SR {SrId} Offer {OfferId}: {Decision} — {Reason}",
                sr.Id, offer.Id, economics.Decision, economics.Reason);
            throw new AizenBusinessException(
                (int)Aizen.Modules.Payment.Abstraction.Enum.PaymentErrorCode.ServiceRequestEconomicsRejected);
        }

        // Approved / ApprovedWithAdjustment — commit acceptance using the snapshot's amounts (§19.11).
        offer.Accept();
        _offerRepository.Update(offer);

        sr.ChangeStatus(ServiceRequestStatus.OfferAccepted);
        var history = ServiceRequestStatusHistoryEntity.Create(
            sr.Id, prevStatus, ServiceRequestStatus.OfferAccepted,
            "Offer accepted", currentUserId, ServiceRequestActorType.Owner);
        sr.AddStatusHistory(history);

        if (economics.TransactionId is { } txId)
            sr.SetPaymentTransaction(txId);

        _logger.LogInformation(
            "Escrow created for SR {SrId} Offer {OfferId}: TransactionId={TxId} Snapshot={SnapId} " +
            "CustomerTotal={Total} ProviderNet={Net} {Currency}",
            sr.Id, offer.Id, economics.TransactionId, economics.EconomicsSnapshotId,
            economics.CustomerTotalAmount, economics.ProviderNetTotal, offer.CurrencyCode);

        _srRepository.Update(sr);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, offer.ProviderProfileId,
            ServiceRequestRealtimeEventType.OfferAccepted, offer.ToDto(),
            currentUserId, ServiceRequestActorType.Owner, cancellationToken);

        // The realtime push above never leaves this process. This is the single most valuable thing a provider can
        // be told — their bid won — so it has to go on the bus, where the provider BFF and Notification can hear it.
        await _messagePublisher.PublishAsync(new ServiceRequestOfferAcceptedMessage
        {
            ServiceRequestId = sr.Id,
            OfferId = offer.Id,
            OwnerUserId = sr.OwnerUserId,
            ProviderProfileId = offer.ProviderProfileId,
        }, cancellationToken);

        // Lifecycle system message (idempotent)
        if (!await _msgRepository.HasSystemMessageAsync(sr.Id, "OFFER_ACCEPTED", cancellationToken))
        {
            var sysMsg = ServiceRequestMessageEntity.Create(
                sr.Id, currentUserId, ServiceRequestMessageSenderType.System,
                ServiceRequestMessageType.StatusChange, "OFFER_ACCEPTED", null);
            await _msgRepository.AddAsync(sysMsg, cancellationToken);

            await _messagePublisher.PublishAsync(new Abstraction.Message.ServiceRequestMessageSentMessage
            {
                ServiceRequestId = sr.Id, MessageId = sysMsg.Id, SenderUserId = currentUserId,
                SenderType = ServiceRequestMessageSenderType.System, ProviderProfileId = offer.ProviderProfileId,
                Content = sysMsg.Content, MessageType = sysMsg.MessageType, OccurredAt = DateTimeOffset.UtcNow
            }, cancellationToken);
        }

        return new AcceptServiceRequestOfferResponse(offer.Id, sr.Id);
    }

    // ── SR → Payment P8 request mapping (mirrors the S7 preview projection; Discount items excluded) ─────────
    /// <summary>
    /// Maps the accepted offer's priced lines to the BE-P8 acceptance-economics request. LineType/eligibility reuse the
    /// S7 preview maps; <c>ItemType</c>/<c>PricingMethod</c> are the raw SR enum ints (as S8 stores). Per line:
    /// <c>LineGrossBeforeDiscount = LineProviderRevenue = Max(LineSubtotal − DiscountAmount, 0)</c> (post-provider-discount,
    /// pre-tax); <c>LineVat = TaxAmount</c>. Idempotency key = <c>SR-{srId}-OFFER-{offerId}</c>.
    /// </summary>
    public static CalculateServiceRequestEconomicsRemoteCallRequest BuildEconomicsRequest(
        ServiceRequestEntity sr, ServiceRequestOfferEntity offer,
        IReadOnlyDictionary<string, List<CalculateServiceRequestEconomicsAttributeDto>>? attributesByLineRef = null)
    {
        // S3 — the frozen submit-time rate per source currency (empty for a TRY-only offer). Keyed by source currency.
        var fxByCurrency = offer.FxSnapshots
            .GroupBy(f => f.SourceCurrencyCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // S3 — once any line has been converted, the whole offer settles in TRY and the economics runs in TRY. An offer
        // with no conversion (TRY-only, or a legacy pre-S3 offer) keeps its own currency verbatim — byte-identical passthrough.
        var wasConverted = offer.FxSnapshots.Count > 0 || offer.Items.Any(i => i.SourceUnitPrice.HasValue);
        var settlementCurrency = wasConverted ? Domain.Entities.Offer.OfferFxConstants.SettlementCurrency : offer.CurrencyCode;

        var lines = offer.Items
            .Where(i => i.ItemType != SrEnum.ServiceRequestOfferItemType.Discount)
            .Select(i =>
            {
                var providerRevenue = Math.Max(i.LineSubtotal - i.DiscountAmount, 0m);
                var lineRef = i.Id.ToString();
                return new CalculateServiceRequestEconomicsLineDto
                {
                    LineRef                 = lineRef,
                    ItemType                = (int)i.ItemType,
                    PricingMethod           = (int)i.PricingMethod,
                    CommissionLineType      = GetOfferCommissionPreviewQueryHandler.MapLineType(i.ItemType),
                    CommissionEligibility   = GetOfferCommissionPreviewQueryHandler.MapEligibility(i.CommissionEligibility),
                    ProductCode             = null,
                    LineGrossBeforeDiscount = providerRevenue,
                    LineVat                 = i.TaxAmount,
                    CommissionBaseAmount    = i.CommissionBaseAmount,
                    LineProviderRevenue     = providerRevenue,
                    DiscountEligible        = i.LineDiscountEligibility != SrEnum.LineDiscountEligibility.Exempt,   // BE-S6
                    Attributes              = attributesByLineRef?.GetValueOrDefault(lineRef) ?? new(),            // S2d
                    Fx                      = BuildLineFx(i, fxByCurrency),                                        // S3
                };
            })
            .ToList();

        return new CalculateServiceRequestEconomicsRemoteCallRequest
        {
            IdempotencyKey    = $"SR-{sr.Id}-OFFER-{offer.Id}",
            ServiceRequestId  = sr.Id,
            OfferId           = offer.Id,
            ProviderProfileId = offer.ProviderProfileId,
            CustomerProfileId = sr.OwnerUserId,
            CustomerPlanId    = null,
            CategoryCode      = sr.ServiceCategoryCode,
            CurrencyCode      = settlementCurrency,   // S3 — TRY once converted; the offer's own currency otherwise
            Lines             = lines,
        };
    }

    /// <summary>
    /// S3 — builds the frozen per-line FX record for the acceptance snapshot from the line's converted price + the offer's
    /// submit-time rate snapshot (keyed by source currency). Null for a settlement-native line (no <c>SourceUnitPrice</c>) or
    /// when — defensively — no rate row exists for the line's currency (the line economics is already TRY regardless).
    /// </summary>
    private static CalculateServiceRequestEconomicsLineFxDto? BuildLineFx(
        ServiceRequestOfferItemEntity item,
        IReadOnlyDictionary<string, Domain.Entities.Offer.OfferFxSnapshotEntity> fxByCurrency)
    {
        if (item.SourceUnitPrice is not { } sourceUnitPrice)
            return null;   // settlement-native line — not converted
        if (!fxByCurrency.TryGetValue((item.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant(), out var fx))
            return null;

        return new CalculateServiceRequestEconomicsLineFxDto
        {
            SourceCurrencyCode     = fx.SourceCurrencyCode,
            SettlementCurrencyCode = fx.SettlementCurrencyCode,
            SourceUnitPrice        = sourceUnitPrice,
            AppliedRate            = fx.Rate,
            RateDate               = fx.RateDate,
            ResolvedUnitPrice      = item.UnitPrice,   // the converted TRY unit price the economics ran on
        };
    }
}
