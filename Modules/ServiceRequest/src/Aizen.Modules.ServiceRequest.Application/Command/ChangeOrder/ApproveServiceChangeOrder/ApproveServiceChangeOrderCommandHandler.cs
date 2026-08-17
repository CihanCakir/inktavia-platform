using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ChangeOrder;
using Aizen.Modules.ServiceRequest.Application.Mapping;
using Aizen.Modules.ServiceRequest.Application.Services;
using Aizen.Modules.ServiceRequest.Application.Services.ChangeOrder;
using Aizen.Modules.ServiceRequest.Domain.Entities.ChangeOrder;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.ServiceRequest.Application.Command.ChangeOrder;

/// <summary>
/// BE-S11b — the substantive apply. The customer approves a proposed change order and it is applied in the SAME operation,
/// reusing the P8/P9/P10 rails for the INCREMENTAL amount (§20.13):
/// <list type="bullet">
///   <item><b>Increase</b> → P8 <c>CalculateServiceRequestEconomics</c> over the change-order lines under a DISTINCT context
///   ref <c>SR-{sr}-OFFER-{offer}-CO-{id}</c> → a NEW immutable snapshot + a NEW incremental escrow/split. The accepted
///   snapshot is never touched. On a P5/S9 breach (or a non-split-eligible provider) the change order is <b>Rejected</b> —
///   no snapshot, no collection — exactly like acceptance.</item>
///   <item><b>Decrease</b> → a P10 refund of the delta against the original escrow (no new snapshot, no mutation).</item>
/// </list>
/// Idempotent: Payment is keyed on the CO context ref (a re-apply returns the same escrow/refund, never double-charges), and
/// the CO status short-circuits an already-applied order. Exercisable via API/bus — no owner app required.
/// </summary>
[DocumentationInfo("Approve change order command handler",
    "Applies a customer-approved change order via P8/P9 (increase) or P10 (decrease); idempotent; breach → Rejected.")]
public sealed class ApproveServiceChangeOrderCommandHandler
    : AizenCommandHandler<ApproveServiceChangeOrderCommand, ServiceChangeOrderDto>
{
    private readonly IServiceRequestRepository       _srRepository;
    private readonly IServiceRequestOfferRepository  _offerRepository;
    private readonly IServiceChangeOrderRepository   _changeOrderRepository;
    private readonly IPaymentModuleRemoteCall        _paymentRemoteCall;
    private readonly OfferCalculationService         _calc;
    private readonly IAizenInfoAccessor              _info;
    private readonly IAizenMessagePublisher          _messagePublisher;
    private readonly ILogger<ApproveServiceChangeOrderCommandHandler> _logger;

    public ApproveServiceChangeOrderCommandHandler(
        IServiceRequestRepository srRepository, IServiceRequestOfferRepository offerRepository,
        IServiceChangeOrderRepository changeOrderRepository, IPaymentModuleRemoteCall paymentRemoteCall,
        OfferCalculationService calc, IAizenInfoAccessor info, IAizenMessagePublisher messagePublisher,
        ILogger<ApproveServiceChangeOrderCommandHandler> logger)
    {
        _srRepository = srRepository; _offerRepository = offerRepository;
        _changeOrderRepository = changeOrderRepository; _paymentRemoteCall = paymentRemoteCall;
        _calc = calc; _info = info; _messagePublisher = messagePublisher; _logger = logger;
    }

    public override async Task<ServiceChangeOrderDto?> Handle(ApproveServiceChangeOrderCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var co = await _changeOrderRepository.GetByIdAsync(request.ChangeOrderId, ct)
            ?? throw new InvalidOperationException($"Change order {request.ChangeOrderId} not found.");
        if (co.ServiceRequestId != request.ServiceRequestId)
            throw new AizenBusinessException("SR_CO_MISMATCH");

        // Idempotent fast-paths.
        if (co.Status == ServiceChangeOrderStatus.Applied)
            return co.ToDto();
        if (co.Status is ServiceChangeOrderStatus.Rejected or ServiceChangeOrderStatus.Cancelled)
            throw new AizenBusinessException("SR_CO_TERMINAL");

        var sr = await _srRepository.GetByIdAsync(co.ServiceRequestId, ct)
            ?? throw new InvalidOperationException($"ServiceRequest {co.ServiceRequestId} not found.");
        var offer = await _offerRepository.GetByIdAsync(co.AcceptedOfferId, ct)
            ?? throw new InvalidOperationException($"Offer {co.AcceptedOfferId} not found.");

        // Customer approval (mirrors the N-E owner action). Resume-safe: only transition when still Proposed.
        if (co.Status == ServiceChangeOrderStatus.Proposed)
            co.MarkCustomerApproved(now);

        var rawToken = _info.UserInfoAccessor.UserInfo.AccessToken;

        if (co.Direction == ServiceChangeOrderDirection.Increase)
            await ApplyIncreaseAsync(sr, offer, co, rawToken, now, ct);
        else
            await ApplyDecreaseAsync(sr, offer, co, rawToken, now, ct);

        _changeOrderRepository.Update(co);
        return co.ToDto();
    }

    // ── Increase → P8 incremental snapshot + escrow (new context ref) ─────────────────────────────
    private async Task ApplyIncreaseAsync(
        ServiceRequestEntity sr, ServiceRequestOfferEntity offer, ServiceChangeOrderEntity co,
        string rawToken, DateTime now, CancellationToken ct)
    {
        var economicsRequest = ServiceChangeOrderEconomics.BuildIncrementalEconomicsRequest(sr, offer, co, _calc);
        var economics = await _paymentRemoteCall.CalculateServiceRequestEconomicsAsync(
            economicsRequest, $"Bearer {rawToken}", ct);

        // A non-split-eligible provider or a P5/S9 breach → the change order is Rejected (no snapshot, no collection).
        if (!economics.ProviderSplitEligible || !economics.CanProceed)
        {
            var reason = !economics.ProviderSplitEligible
                ? "Provider not split-eligible."
                : $"Incremental economics {economics.Decision}: {economics.Reason}";
            co.Reject(reason, now);
            _logger.LogWarning(
                "Change order {CoId} rejected on apply (SR {SrId} Offer {OfferId}): {Reason}",
                co.Id, sr.Id, offer.Id, reason);
            await PublishRejectedAsync(sr, offer, co, reason, ct);
            return;
        }

        co.MarkApplied(
            economicsSnapshotId:  economics.EconomicsSnapshotId ?? 0,
            paymentTransactionId: economics.TransactionId ?? 0,
            customerTotal:        economics.CustomerTotalAmount,
            providerNet:          economics.ProviderNetTotal,
            utcNow:               now);

        _logger.LogInformation(
            "Change order {CoId} applied (Increase) for SR {SrId} Offer {OfferId}: +{Total} snapshot={Snap} tx={Tx}.",
            co.Id, sr.Id, offer.Id, economics.CustomerTotalAmount, economics.EconomicsSnapshotId, economics.TransactionId);

        await PublishAppliedAsync(sr, offer, co, ct);
    }

    // ── Decrease → P10 refund of the delta against the original escrow ────────────────────────────
    private async Task ApplyDecreaseAsync(
        ServiceRequestEntity sr, ServiceRequestOfferEntity offer, ServiceChangeOrderEntity co,
        string rawToken, DateTime now, CancellationToken ct)
    {
        var reductionAmount = ServiceChangeOrderEconomics.ComputeLineGrandTotal(offer, co, _calc);
        if (reductionAmount <= 0m)
        {
            co.Reject("Reduction amount is not positive.", now);
            await PublishRejectedAsync(sr, offer, co, "Reduction amount is not positive.", ct);
            return;
        }

        var result = await _paymentRemoteCall.ApplyChangeOrderReductionAsync(new ApplyChangeOrderReductionRemoteCallRequest
        {
            ServiceRequestId = sr.Id,
            AcceptedOfferId  = offer.Id,
            ChangeOrderId    = co.Id,
            ReductionAmount  = reductionAmount,
            RefundReasonCode = (int)Aizen.Modules.Payment.Abstraction.Enum.RefundReason.PriceAdjustment,
            Notes            = co.Reason,
        }, $"Bearer {rawToken}", ct);

        if (!result.Applied)
        {
            var reason = result.Message ?? "No escrow to reduce.";
            co.Reject(reason, now);
            _logger.LogWarning("Change order {CoId} (Decrease) not applied for SR {SrId}: {Reason}", co.Id, sr.Id, reason);
            await PublishRejectedAsync(sr, offer, co, reason, ct);
            return;
        }

        co.MarkAppliedAsReduction(
            originalTransactionId: result.TransactionId ?? 0,
            refundRecordId:        result.RefundRecordId,
            refundedAmount:        result.RefundedAmount,
            utcNow:                now);

        _logger.LogInformation(
            "Change order {CoId} applied (Decrease) for SR {SrId} Offer {OfferId}: -{Amount} refund={RId}.",
            co.Id, sr.Id, offer.Id, result.RefundedAmount, result.RefundRecordId);

        await PublishAppliedAsync(sr, offer, co, ct);
    }

    private Task PublishAppliedAsync(ServiceRequestEntity sr, ServiceRequestOfferEntity offer, ServiceChangeOrderEntity co, CancellationToken ct)
        => _messagePublisher.PublishAsync(new ServiceChangeOrderAppliedMessage
        {
            ServiceRequestId     = sr.Id,
            ChangeOrderId        = co.Id,
            AcceptedOfferId      = offer.Id,
            OwnerUserId          = sr.OwnerUserId,
            ProviderProfileId    = offer.ProviderProfileId,
            Direction            = co.Direction,
            AppliedCustomerTotal = co.AppliedCustomerTotal,
        }, ct);

    private Task PublishRejectedAsync(ServiceRequestEntity sr, ServiceRequestOfferEntity offer, ServiceChangeOrderEntity co, string reason, CancellationToken ct)
        => _messagePublisher.PublishAsync(new ServiceChangeOrderRejectedMessage
        {
            ServiceRequestId  = sr.Id,
            ChangeOrderId     = co.Id,
            AcceptedOfferId   = offer.Id,
            OwnerUserId       = sr.OwnerUserId,
            ProviderProfileId = offer.ProviderProfileId,
            Reason            = reason,
        }, ct);
}
