using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Abstraction.Model;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.RefundAllocation;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.ApplyChangeOrderReduction;

/// <summary>
/// BE-S11b — the reduction half of the change-order apply. Refunds the change order's delta against the SR's original
/// escrow, reusing the gateway refund + <see cref="RefundAllocationService"/> (the §7.5 nine-field allocation runs
/// deterministically off the ORIGINAL snapshot — no bespoke math, the accepted snapshot is never mutated). Idempotent on
/// the change-order context ref <c>SR-{sr}-OFFER-{offer}-CO-{id}</c> (stamped on the refund record's AdminNote) so a
/// re-apply never double-refunds. Mirrors the dispute payer-refund branch.
/// </summary>
[DocumentationInfo("Apply change-order reduction command handler",
    "Refunds a change-order reduction via the P10 rails; idempotent on SR-{sr}-OFFER-{offer}-CO-{id}.")]
public sealed class ApplyChangeOrderReductionCommandHandler
    : AizenCommandHandler<ApplyChangeOrderReductionCommand, ApplyChangeOrderReductionRemoteCallResponse>
{
    private readonly IPaymentTransactionRepository                     _transactions;
    private readonly PaymentGatewayResolver                            _gatewayResolver;
    private readonly RefundAllocationService                          _allocationService;
    private readonly IAizenMessagePublisher                           _publisher;
    private readonly ILogger<ApplyChangeOrderReductionCommandHandler> _logger;

    public ApplyChangeOrderReductionCommandHandler(
        IAizenUnitOfWork<PaymentDbContext>                unitOfWork,
        IPaymentTransactionRepository                     transactions,
        PaymentGatewayResolver                            gatewayResolver,
        RefundAllocationService                           allocationService,
        IAizenMessagePublisher                            publisher,
        ILogger<ApplyChangeOrderReductionCommandHandler>  logger)
    {
        _transactions      = transactions;
        _gatewayResolver   = gatewayResolver;
        _allocationService = allocationService;
        _publisher         = publisher;
        _logger            = logger;
    }

    /// <summary>Change-order context ref — the same key the increase path uses for its incremental escrow.</summary>
    public static string ContextRef(long serviceRequestId, long acceptedOfferId, long changeOrderId)
        => $"SR-{serviceRequestId}-OFFER-{acceptedOfferId}-CO-{changeOrderId}";

    public override async Task<ApplyChangeOrderReductionRemoteCallResponse?> Handle(
        ApplyChangeOrderReductionCommand command, CancellationToken ct)
    {
        var request    = command.Request;
        var contextRef = ContextRef(request.ServiceRequestId, request.AcceptedOfferId, request.ChangeOrderId);

        // Locate the SR's original escrow. No escrow ever held → nothing to reduce.
        var txHead = await _transactions.GetByContextAsync(
            TransactionContextType.ServiceRequest, request.ServiceRequestId, ct);
        if (txHead is null)
        {
            _logger.LogInformation(
                "ApplyChangeOrderReduction: no transaction for SR {SRId} — change order {CoId} recorded, no money moved.",
                request.ServiceRequestId, request.ChangeOrderId);
            return new ApplyChangeOrderReductionRemoteCallResponse
            { Applied = false, Message = "No transaction for this service request." };
        }

        var tx = await _transactions.GetByIdWithRefundsAsync(txHead.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.TransactionNotFound);

        // Idempotent: a prior apply already refunded this change order.
        var existing = await _transactions.GetRefundRecordsAsync(tx.Id, ct);
        var prior = existing.FirstOrDefault(r =>
            r.Status != TransactionRefundStatus.Failed &&
            r.AdminNote is not null &&
            r.AdminNote.StartsWith(contextRef, StringComparison.Ordinal));
        if (prior is not null)
        {
            _logger.LogInformation(
                "ApplyChangeOrderReduction: refund already applied for change order {CoId} (Tx {TxId} Refund {RId}). Idempotent.",
                request.ChangeOrderId, tx.Id, prior.Id);
            return new ApplyChangeOrderReductionRemoteCallResponse
            {
                Applied = true, AlreadyApplied = true, RefundedAmount = prior.Amount,
                RefundRecordId = prior.Id, TransactionId = tx.Id,
            };
        }

        if (tx.Status is not (PaymentTransactionStatus.Captured
                              or PaymentTransactionStatus.Released
                              or PaymentTransactionStatus.PartiallyRefunded))
            throw new AizenBusinessException((int)PaymentErrorCode.TransactionInvalidState);

        var refundable = tx.GrossAmount - tx.TotalRefundedAmount;

        // §S13b guard reused — strictly positive and ≤ refundable.
        if (!DisputeRefundGuard.IsRefundableAmountValid(request.ReductionAmount, refundable))
            throw new AizenBusinessException((int)PaymentErrorCode.RefundAmountExceedsMaximum);

        var amount     = request.ReductionAmount;
        var reason     = (RefundReason)request.RefundReasonCode;
        var cause      = RefundCauseMap.FromReason(reason);
        var refundType = amount >= refundable ? RefundType.Full : RefundType.Partial;
        var adminNote  = $"{contextRef}: change order reduction. {request.Notes}".Trim();

        var gateway = _gatewayResolver.Resolve();
        var gatewayResult = await gateway.RefundAsync(new RefundInput
        {
            TransactionId            = tx.Id,
            GatewayReference         = tx.GatewayReference ?? string.Empty,
            GatewayItemTransactionId = tx.GatewayItemTransactionId,
            RefundAmount             = amount,
            Currency                 = tx.CurrencyCode,
            Reason                   = reason,
            AdminNote                = adminNote,
        }, ct);

        if (!gatewayResult.Processed)
            throw new AizenBusinessException((int)PaymentErrorCode.TransactionInvalidState);

        var record = TransactionRefundRecord.Create(
            paymentTransactionId: tx.Id,
            refundCode:           GenerateRefundCode(),
            amount:               amount,
            currencyCode:         tx.CurrencyCode,
            refundType:           refundType,
            reason:               reason,
            adminNote:            adminNote);   // AdminNote starts with contextRef → idempotency anchor
        record.MarkProcessed(gatewayResult.GatewayRefundReference, adminNote);
        await _transactions.AddRefundRecordAsync(record, ct);

        tx.ApplyRefund(record);
        _transactions.Update(tx);

        // ── Reuse the P10 snapshot-driven allocation (§7.5). No bespoke refund math; the ORIGINAL snapshot is read, not mutated. ──
        await _allocationService.ApplyAsync(
            tx, record, requestedRefundAmount: amount, cause: cause, restoreBenefit: true, ct: ct);
        // SaveChanges handled by the command decorator.

        _ = _publisher.PublishAsync(new PaymentRefundedMessage
        {
            TransactionId   = tx.Id,
            TransactionCode = tx.TransactionCode,
            ContextType     = tx.ContextType,
            ContextId       = tx.ContextId,
            ContextSubId    = tx.ContextSubId,
            PayerProfileId  = tx.PayerProfileId,
            RefundedAmount  = amount,
            OriginalAmount  = tx.GrossAmount,
            CurrencyCode    = tx.CurrencyCode,
            IsPartial       = refundType == RefundType.Partial,
            Reason          = reason.ToString(),
            RefundedAtUtc   = DateTime.UtcNow,
        }, ct).ContinueWith(t =>
        {
            if (t.IsFaulted)
                _logger.LogError(t.Exception, "Failed to publish PaymentRefundedMessage for Tx {TxId}", tx.Id);
        }, TaskContinuationOptions.OnlyOnFaulted);

        _logger.LogInformation(
            "ApplyChangeOrderReduction: refund {Amount} applied for change order {CoId} (Tx {TxId} Refund {RId} {Type}).",
            amount, request.ChangeOrderId, tx.Id, record.Id, refundType);

        return new ApplyChangeOrderReductionRemoteCallResponse
        { Applied = true, RefundedAmount = amount, RefundRecordId = record.Id, TransactionId = tx.Id };
    }

    private static string GenerateRefundCode()
        => $"REF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
