using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Payment.Abstraction.Model;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.Payout;
using Aizen.Modules.Payment.Domain.Entities.RefundAllocation;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.ResolveDisputeOutcome;

/// <summary>
/// BE-S13b — the one economics-affecting part of dispute resolution. Given an SR + a dispute outcome it either
/// releases the held escrow to the provider (FavorProviderRelease) or issues a refund to the payer (full / partial /
/// split), reusing the existing gateway + <see cref="RefundAllocationService"/> so the §7.5 nine-field allocation runs
/// deterministically. Idempotent on <c>DISPUTE-{disputeId}</c> (stamped on the refund record's AdminNote / guarded by
/// the payout-exists check) so a re-resolve never double-refunds or double-releases.
/// </summary>
[DocumentationInfo("Resolve dispute outcome command handler",
    "Drives the P10 refund/escrow path for a resolved dispute — reuses the gateway + RefundAllocationService; idempotent on DISPUTE-{id}.")]
public sealed class ResolveDisputeOutcomeCommandHandler
    : AizenCommandHandler<ResolveDisputeOutcomeCommand, ResolveDisputeOutcomeRemoteCallResponse>
{
    private readonly IPaymentTransactionRepository                _transactions;
    private readonly IPayoutRecordRepository                      _payouts;
    private readonly PaymentGatewayResolver                       _gatewayResolver;
    private readonly RefundAllocationService                      _allocationService;
    private readonly IAizenMessagePublisher                       _publisher;
    private readonly ILogger<ResolveDisputeOutcomeCommandHandler> _logger;

    public ResolveDisputeOutcomeCommandHandler(
        IAizenUnitOfWork<PaymentDbContext>            unitOfWork,
        IPaymentTransactionRepository                 transactions,
        IPayoutRecordRepository                       payouts,
        PaymentGatewayResolver                        gatewayResolver,
        RefundAllocationService                       allocationService,
        IAizenMessagePublisher                        publisher,
        ILogger<ResolveDisputeOutcomeCommandHandler>  logger)
    {
        _transactions      = transactions;
        _payouts           = payouts;
        _gatewayResolver   = gatewayResolver;
        _allocationService = allocationService;
        _publisher         = publisher;
        _logger            = logger;
    }

    public override async Task<ResolveDisputeOutcomeRemoteCallResponse?> Handle(
        ResolveDisputeOutcomeCommand command, CancellationToken ct)
    {
        var request    = command.Request;
        var contextRef = DisputeRefundGuard.ContextRef(request.DisputeId);

        // Locate the SR's transaction. No escrow was ever held → nothing to move (a dispute on an unpaid SR).
        var txHead = await _transactions.GetByContextAsync(
            TransactionContextType.ServiceRequest, request.ServiceRequestId, ct);
        if (txHead is null)
        {
            _logger.LogInformation(
                "ResolveDisputeOutcome: no transaction for SR {SRId} — dispute {DisputeId} recorded, no money moved.",
                request.ServiceRequestId, request.DisputeId);
            return new ResolveDisputeOutcomeRemoteCallResponse
            { Applied = false, Message = "No transaction for this service request." };
        }

        // Reload with refunds so the idempotency guard + ApplyRefund domain checks can run.
        var tx = await _transactions.GetByIdWithRefundsAsync(txHead.Id, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.TransactionNotFound);

        return request.ReleaseToProvider
            ? await ReleaseAsync(tx, request, ct)
            : await RefundAsync(tx, request, contextRef, ct);
    }

    // ── FavorProviderRelease → release escrow to provider (no refund) ─────────────────────────────
    private async Task<ResolveDisputeOutcomeRemoteCallResponse> ReleaseAsync(
        PaymentTransactionEntity tx, ResolveDisputeOutcomeRemoteCallRequest request,
        CancellationToken ct)
    {
        // Idempotent: escrow already released to the provider (either by a prior resolve or the completion flow).
        if (tx.Status == PaymentTransactionStatus.Released || await _payouts.ActivePayoutExistsAsync(tx.Id, ct))
        {
            _logger.LogInformation(
                "ResolveDisputeOutcome: escrow already released for Tx {TxId} (dispute {DisputeId}). Idempotent no-op.",
                tx.Id, request.DisputeId);
            return new ResolveDisputeOutcomeRemoteCallResponse
            { Applied = true, AlreadyApplied = true, EscrowReleased = true, Message = "Escrow already released." };
        }

        if (tx.Status != PaymentTransactionStatus.Captured)
            return new ResolveDisputeOutcomeRemoteCallResponse
            { Applied = false, Message = $"Transaction is {tx.Status}; no escrow to release." };

        var gateway = _gatewayResolver.Resolve();
        var payoutResult = await gateway.ReleaseEscrowAsync(new ReleaseEscrowInput
        {
            TransactionId            = tx.Id,
            GatewayReference         = tx.GatewayReference ?? string.Empty,
            GatewayItemTransactionId = tx.GatewayItemTransactionId,
            ProviderNetAmount        = tx.NetPayoutAmount,
            AdminNote                = $"DISPUTE-{request.DisputeId} resolved for provider. {request.Notes}".Trim(),
        }, ct);

        if (!payoutResult.Processed)
            throw new AizenBusinessException((int)PaymentErrorCode.EscrowReleaseInvalidState);

        tx.Release();
        _transactions.Update(tx);

        var payout = PayoutRecordEntity.Create(
            providerProfileId:    tx.RecipientProfileId ?? 0,
            paymentTransactionId: tx.Id,
            amount:               tx.NetPayoutAmount,
            currencyCode:         tx.CurrencyCode,
            gatewayProvider:      gateway.ProviderKey);
        payout.MarkCompleted(payoutResult.GatewayPayoutId, $"DISPUTE-{request.DisputeId}");
        await _payouts.AddAsync(payout, ct);
        // SaveChanges handled by the command decorator.

        _logger.LogInformation(
            "ResolveDisputeOutcome: escrow released to provider for Tx {TxId} (dispute {DisputeId}) PayoutId={PayoutId}.",
            tx.Id, request.DisputeId, payout.Id);

        return new ResolveDisputeOutcomeRemoteCallResponse
        { Applied = true, EscrowReleased = true, PayoutRecordId = payout.Id };
    }

    // ── FavorPayer* / Split → refund to payer (reuses gateway + RefundAllocationService) ──────────
    private async Task<ResolveDisputeOutcomeRemoteCallResponse> RefundAsync(
        PaymentTransactionEntity tx, ResolveDisputeOutcomeRemoteCallRequest request,
        string contextRef, CancellationToken ct)
    {
        // Idempotent: a prior resolve already claimed this dispute's refund.
        var existing = await _transactions.GetRefundRecordsAsync(tx.Id, ct);
        var prior = existing.FirstOrDefault(r =>
            r.Status != TransactionRefundStatus.Failed &&
            r.AdminNote is not null &&
            r.AdminNote.StartsWith(contextRef, StringComparison.Ordinal));
        if (prior is not null)
        {
            _logger.LogInformation(
                "ResolveDisputeOutcome: refund already applied for dispute {DisputeId} (Tx {TxId} Refund {RId}). Idempotent.",
                request.DisputeId, tx.Id, prior.Id);
            return new ResolveDisputeOutcomeRemoteCallResponse
            { Applied = true, AlreadyApplied = true, RefundedAmount = prior.Amount, RefundRecordId = prior.Id };
        }

        if (tx.Status is not (PaymentTransactionStatus.Captured
                              or PaymentTransactionStatus.Released
                              or PaymentTransactionStatus.PartiallyRefunded))
            throw new AizenBusinessException((int)PaymentErrorCode.TransactionInvalidState);

        var refundable = tx.GrossAmount - tx.TotalRefundedAmount;
        var amount     = request.FullRefund ? refundable : (request.RefundAmount ?? 0m);

        // §S13b — validate the amount against the transaction (≤ refundable, strictly positive).
        if (!DisputeRefundGuard.IsRefundableAmountValid(amount, refundable))
            throw new AizenBusinessException((int)PaymentErrorCode.RefundAmountExceedsMaximum);

        var reason     = (RefundReason)request.RefundReasonCode;
        var cause      = RefundCauseMap.FromReason(reason);
        var refundType = amount >= refundable ? RefundType.Full : RefundType.Partial;
        var adminNote  = $"{contextRef}: dispute resolved for payer. {request.Notes}".Trim();

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
            adminNote:            adminNote);        // AdminNote starts with contextRef → idempotency anchor
        record.MarkProcessed(gatewayResult.GatewayRefundReference, adminNote);
        await _transactions.AddRefundRecordAsync(record, ct);

        tx.ApplyRefund(record);
        _transactions.Update(tx);

        // ── Reuse the P10 snapshot-driven allocation (§7.5 nine-field). No bespoke refund math here. ──
        await _allocationService.ApplyAsync(
            tx, record, requestedRefundAmount: amount, cause: cause, restoreBenefit: true, ct: ct);
        // SaveChanges handled by the command decorator.

        _ = _publisher.PublishAsync(new PaymentRefundedMessage
        {
            TransactionId  = tx.Id,
            TransactionCode = tx.TransactionCode,
            ContextType    = tx.ContextType,
            ContextId      = tx.ContextId,
            ContextSubId   = tx.ContextSubId,
            PayerProfileId = tx.PayerProfileId,
            RefundedAmount = amount,
            OriginalAmount = tx.GrossAmount,
            CurrencyCode   = tx.CurrencyCode,
            IsPartial      = refundType == RefundType.Partial,
            Reason         = reason.ToString(),
            RefundedAtUtc  = DateTime.UtcNow,
        }, ct).ContinueWith(t =>
        {
            if (t.IsFaulted)
                _logger.LogError(t.Exception, "Failed to publish PaymentRefundedMessage for Tx {TxId}", tx.Id);
        }, TaskContinuationOptions.OnlyOnFaulted);

        _logger.LogInformation(
            "ResolveDisputeOutcome: refund {Amount} applied for dispute {DisputeId} (Tx {TxId} Refund {RId} {Type}).",
            amount, request.DisputeId, tx.Id, record.Id, refundType);

        return new ResolveDisputeOutcomeRemoteCallResponse
        { Applied = true, RefundedAmount = amount, RefundRecordId = record.Id };
    }

    private static string GenerateRefundCode()
        => $"REF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
