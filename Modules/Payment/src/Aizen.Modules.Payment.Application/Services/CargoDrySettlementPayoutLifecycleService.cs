using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Interface;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// Drives the payout record lifecycle for CargoDry sell-through settlement payouts.
/// All operations record lifecycle state only — no gateway or Iyzico calls are made.
/// Phase 4D (July 2026).
/// </summary>
[DocumentationInfo("CargoDry settlement payout lifecycle service",
    "Records Approve, Processing, Complete, and Fail transitions on PayoutRecordEntity for " +
    "CargoDry ConsignmentSellThrough settlement payouts. " +
    "Does NOT call Iyzico. Does NOT initiate bank transfers. " +
    "PayoutRecord is the Payment module source of truth; " +
    "CargoDry settlement closure (Settled) is driven by CompleteCargoDrySettlementPayoutCommandHandler " +
    "after verifying this service returns Completed. Phase 4D.")]
public sealed class CargoDrySettlementPayoutLifecycleService : ICargoDrySettlementPayoutLifecycleService
{
    private readonly IPayoutRecordRepository _payoutRepo;

    public CargoDrySettlementPayoutLifecycleService(IPayoutRecordRepository payoutRepo)
        => _payoutRepo = payoutRepo;

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static CargoDryPayoutLifecycleResultDto MapToResult(
        Aizen.Modules.Payment.Domain.Entities.Payout.PayoutRecordEntity payout,
        bool   alreadyCompleted = false,
        string message          = "")
    {
        return new CargoDryPayoutLifecycleResultDto
        {
            PayoutRecordId      = payout.Id,
            PayoutStatus        = payout.Status,
            ProviderProfileId   = payout.ProviderProfileId,
            Amount              = payout.Amount,
            CurrencyCode        = payout.CurrencyCode,
            ExternalReference   = payout.GatewayPayoutId,
            ApprovedAtUtc       = payout.ApprovedAtUtc,
            ApprovedByUserId    = payout.ApprovedByUserId,
            ProcessingAtUtc     = payout.ProcessingAtUtc,
            CompletedAtUtc      = payout.Status == PayoutStatus.Completed ? payout.ProcessedAt : null,
            CompletedByUserId   = payout.CompletedByUserId,
            FailedAtUtc         = payout.FailedAtUtc,
            FailedByUserId      = payout.FailedByUserId,
            FailureReason       = payout.FailureReason,
            AdminNote           = payout.AdminNote,
            AlreadyCompleted    = alreadyCompleted,
            Message             = message,
        };
    }

    private async Task<Aizen.Modules.Payment.Domain.Entities.Payout.PayoutRecordEntity> LoadAndValidateAsync(
        long payoutRecordId, long sourceSettlementId, CancellationToken ct)
    {
        var payout = await _payoutRepo.GetByIdAsync(payoutRecordId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.PayoutRecordNotFound,
                $"Payout record {payoutRecordId} not found.");

        // Verify this payout belongs to the given CargoDry settlement (cross-module guard)
        if (payout.SourceType != "CargoDrySettlement" || payout.SourceId != sourceSettlementId)
            throw new AizenBusinessException((int)PaymentErrorCode.PayoutRecordNotFound,
                $"Payout record {payoutRecordId} does not belong to settlement {sourceSettlementId}.");

        return payout;
    }

    // ── Public interface ──────────────────────────────────────────────────────────

    public async Task<CargoDryPayoutLifecycleResultDto> GetPayoutStateAsync(
        long payoutRecordId, long sourceSettlementId, CancellationToken ct = default)
    {
        var payout = await LoadAndValidateAsync(payoutRecordId, sourceSettlementId, ct);
        return MapToResult(payout, message: "Payout state retrieved.");
    }

    public async Task<CargoDryPayoutLifecycleResultDto> ApproveAsync(
        long payoutRecordId, long approvedByUserId, string? note, CancellationToken ct = default)
    {
        // Source settlement validation is performed by the CargoDry command handler upstream.
        // Here we only need to load and transition the payout record.
        var payout = await _payoutRepo.GetByIdAsync(payoutRecordId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.PayoutRecordNotFound,
                $"Payout record {payoutRecordId} not found.");

        payout.Approve(approvedByUserId, DateTime.UtcNow, note);
        _payoutRepo.Update(payout);
        await _payoutRepo.SaveChangesAsync(ct);

        return MapToResult(payout, message: "Payout approved for disbursement.");
    }

    public async Task<CargoDryPayoutLifecycleResultDto> MarkProcessingAsync(
        long payoutRecordId, long processedByUserId, string? externalReference, string? note,
        CancellationToken ct = default)
    {
        var payout = await _payoutRepo.GetByIdAsync(payoutRecordId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.PayoutRecordNotFound);

        payout.MarkProcessingByAdmin(processedByUserId, DateTime.UtcNow, externalReference, note);
        _payoutRepo.Update(payout);
        await _payoutRepo.SaveChangesAsync(ct);

        return MapToResult(payout, message: "Payout marked as processing.");
    }

    public async Task<CargoDryPayoutLifecycleResultDto> CompleteManualAsync(
        long payoutRecordId, long completedByUserId, string manualPaymentReference, string? note,
        CancellationToken ct = default)
    {
        var payout = await _payoutRepo.GetByIdAsync(payoutRecordId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.PayoutRecordNotFound);

        if (payout.Status == PayoutStatus.Completed)
        {
            // Idempotent: already completed, return existing state
            return MapToResult(payout, alreadyCompleted: true, message: "Payout was already completed.");
        }

        payout.MarkCompletedManual(completedByUserId, DateTime.UtcNow, manualPaymentReference, note);
        _payoutRepo.Update(payout);
        await _payoutRepo.SaveChangesAsync(ct);

        return MapToResult(payout, message: "Payout completed successfully.");
    }

    public async Task<CargoDryPayoutLifecycleResultDto> FailAsync(
        long payoutRecordId, long failedByUserId, string failureReason,
        string? externalReference, string? note, CancellationToken ct = default)
    {
        var payout = await _payoutRepo.GetByIdAsync(payoutRecordId, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.PayoutRecordNotFound);

        payout.MarkFailedByAdmin(failedByUserId, DateTime.UtcNow, failureReason, externalReference, note);
        _payoutRepo.Update(payout);
        await _payoutRepo.SaveChangesAsync(ct);

        return MapToResult(payout, message: "Payout recorded as failed.");
    }
}
