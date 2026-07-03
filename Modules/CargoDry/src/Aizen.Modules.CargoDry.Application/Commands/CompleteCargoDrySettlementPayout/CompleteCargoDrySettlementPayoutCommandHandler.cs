using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.Payment.Abstraction.Interface;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Commands.CompleteCargoDrySettlementPayout;

[DocumentationInfo("CompleteCargoDrySettlementPayoutCommandHandler",
    "Records the successful manual payout completion for a CargoDry sell-through settlement. " +
    "This is the ONLY command allowed to mark the settlement as Settled. " +
    "Flow: validates prerequisites (Scheduled, PayoutRecordId, InvoiceId), " +
    "calls ICargoDrySettlementPayoutLifecycleService.CompleteManualAsync() on the Payment module, " +
    "then calls settlement.MarkPayoutCompleted() which advances Status to Settled. " +
    "Idempotent — if PayoutRecord is already Completed and settlement already Settled, returns existing data. " +
    "ManualPaymentReference is required (bank transfer ref or payment confirmation). " +
    "No Iyzico call. No automatic transfer. Phase 4D (July 2026).")]
public sealed class CompleteCargoDrySettlementPayoutCommandHandler
    : AizenCommandHandler<CompleteCargoDrySettlementPayoutCommand, CompleteCargoDrySettlementPayoutResponse>
{
    private readonly ICargoDrySellThroughSettlementRepository         _settlements;
    private readonly ICargoDrySettlementPayoutLifecycleService        _lifecycleService;
    private readonly ILogger<CompleteCargoDrySettlementPayoutCommandHandler> _logger;

    public CompleteCargoDrySettlementPayoutCommandHandler(
        ICargoDrySellThroughSettlementRepository              settlements,
        ICargoDrySettlementPayoutLifecycleService             lifecycleService,
        ILogger<CompleteCargoDrySettlementPayoutCommandHandler> logger)
    {
        _settlements      = settlements;
        _lifecycleService = lifecycleService;
        _logger           = logger;
    }

    public override async Task<CompleteCargoDrySettlementPayoutResponse> Handle(
        CompleteCargoDrySettlementPayoutCommand request, CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;

        // ── Load settlement ────────────────────────────────────────────────────
        var settlement = await _settlements.GetByIdAsync(request.SettlementId, ct)
            ?? throw new AizenBusinessException(
                $"Sell-through settlement {request.SettlementId} not found.");

        // ── Idempotency: if already Settled, return existing data ──────────────
        if (settlement.Status == CargoDrySellThroughSettlementStatus.Settled)
        {
            _logger.LogInformation(
                "Settlement {Id} ({Code}) is already Settled. PayoutCompletionReference={Ref}",
                settlement.Id, settlement.SettlementCode, settlement.PayoutCompletionReference);

            // Still call the lifecycle service to get current payout state for the response
            if (settlement.PayoutRecordId.HasValue)
            {
                var existingPayoutResult = await _lifecycleService.GetPayoutStateAsync(
                    settlement.PayoutRecordId.Value, settlement.Id, ct);

                return new CompleteCargoDrySettlementPayoutResponse
                {
                    Settlement       = BuildSettlementDto(settlement),
                    PayoutResult     = existingPayoutResult,
                    AlreadyCompleted = true,
                };
            }

            throw new AizenBusinessException(
                $"Settlement {settlement.Id} ({settlement.SettlementCode}) is already Settled " +
                "but has no linked PayoutRecord. Data integrity issue — contact system administrator.");
        }

        // ── Status guard: must be Scheduled ───────────────────────────────────
        if (settlement.Status != CargoDrySellThroughSettlementStatus.Scheduled)
            throw new AizenBusinessException(
                $"Settlement {settlement.Id} ({settlement.SettlementCode}) is in {settlement.Status} status. " +
                "Only Scheduled settlements can have payout completed.");

        // ── Phase 4B guard: payout record must exist ──────────────────────────
        if (!settlement.PayoutRecordId.HasValue)
            throw new AizenBusinessException(
                $"Settlement {settlement.Id} ({settlement.SettlementCode}) has no linked PayoutRecord. " +
                "Run PrepareCargoDrySettlementPayment (Phase 4B) first.");

        // ── Phase 4C guard: invoice must exist ────────────────────────────────
        if (!settlement.InvoiceId.HasValue)
            throw new AizenBusinessException(
                $"Settlement {settlement.Id} ({settlement.SettlementCode}) has no linked invoice. " +
                "Run PrepareCargoDrySettlementInvoice (Phase 4C) before completing the payout.");

        // ── Complete payout in Payment module ──────────────────────────────────
        var payoutResult = await _lifecycleService.CompleteManualAsync(
            payoutRecordId:         settlement.PayoutRecordId.Value,
            completedByUserId:      request.CompletedByUserId,
            manualPaymentReference: request.ManualPaymentReference,
            note:                   request.Note,
            ct:                     ct);

        // ── Close the settlement (MarkPayoutCompleted → Status = Settled) ─────
        // This is the ONLY place where settlement.Status transitions to Settled.
        settlement.MarkPayoutCompleted(
            completedByUserId: request.CompletedByUserId,
            completedAtUtc:    nowUtc,
            payoutReference:   request.ManualPaymentReference,
            note:              request.Note);

        await _settlements.SaveChangesAsync(ct);

        _logger.LogInformation(
            "CargoDry settlement payout completed and settlement closed. " +
            "SettlementId={Id} Code={Code} PayoutRecordId={PayoutId} Reference={Ref} Status={Status}",
            settlement.Id, settlement.SettlementCode,
            settlement.PayoutRecordId, request.ManualPaymentReference, settlement.Status);

        return new CompleteCargoDrySettlementPayoutResponse
        {
            Settlement       = BuildSettlementDto(settlement),
            PayoutResult     = payoutResult,
            AlreadyCompleted = payoutResult.AlreadyCompleted,
        };
    }

    private static CargoDrySellThroughSettlementDto BuildSettlementDto(
        Domain.Entities.CargoDrySellThroughSettlementEntity s) => new()
    {
        Id                        = s.Id,
        PublicId                  = s.PublicId,
        SettlementCode            = s.SettlementCode,
        ConsignmentAgreementId    = s.ConsignmentAgreementId,
        ProviderProfileId         = s.ProviderProfileId,
        ProductCode               = s.ProductCode,
        BatchCode                 = s.BatchCode,
        TotalKitCount             = s.TotalKitCount,
        SettledKitCount           = s.SettledKitCount,
        TotalSaleAmount           = s.TotalSaleAmount,
        TotalCommissionAmount     = s.TotalCommissionAmount,
        ProviderPayoutAmount      = s.ProviderPayoutAmount,
        CurrencyCode              = s.CurrencyCode,
        PeriodStartUtc            = s.PeriodStartUtc,
        PeriodEndUtc              = s.PeriodEndUtc,
        Status                    = s.Status,
        StatusName                = s.Status.ToString(),
        ScheduledSettlementDate   = s.ScheduledSettlementDate,
        SettledAtUtc              = s.SettledAtUtc,
        SettledByUserId           = s.SettledByUserId,
        DisputeReason             = s.DisputeReason,
        Note                      = s.Note,
        ReadyForSettlementAtUtc   = s.ReadyForSettlementAtUtc,
        PayoutRecordId            = s.PayoutRecordId,
        PaymentPreparedAtUtc      = s.PaymentPreparedAtUtc,
        PaymentPreparedByUserId   = s.PaymentPreparedByUserId,
        PaymentPreparationNote    = s.PaymentPreparationNote,
        InvoiceId                 = s.InvoiceId,
        InvoicePreparedAtUtc      = s.InvoicePreparedAtUtc,
        InvoicePreparedByUserId   = s.InvoicePreparedByUserId,
        InvoicePreparationNote    = s.InvoicePreparationNote,
        PayoutCompletedAtUtc      = s.PayoutCompletedAtUtc,
        PayoutCompletedByUserId   = s.PayoutCompletedByUserId,
        PayoutCompletionReference = s.PayoutCompletionReference,
        PayoutFailureReason       = s.PayoutFailureReason,
        PayoutLifecycleNote       = s.PayoutLifecycleNote,
        CreatedAtUtc              = s.CreatedAtUtc,
    };
}
