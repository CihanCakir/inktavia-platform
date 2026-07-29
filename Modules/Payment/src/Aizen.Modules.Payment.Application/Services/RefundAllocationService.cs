using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.RefundAllocation;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Aizen.Modules.Payment.Domain.Money;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// BE-P10 — orchestrates the snapshot-driven refund allocation (§7.5) + release-before/after recovery (§7.2/§7.3) +
/// provider negative-balance ledger (§7.4) + benefit restore-once (§19.15). Every reversal comes from the immutable
/// economics snapshot; nothing is recomputed. Called by the refund handlers AFTER the refund record is created.
/// </summary>
public sealed class RefundAllocationService
{
    private readonly IPaymentEconomicsSnapshotRepository _snapshots;
    private readonly IRefundAllocationPolicyRepository   _policies;
    private readonly IRefundAllocationRepository         _allocations;
    private readonly IProviderBalanceRepository          _balances;
    private readonly FinancialLedgerPostingService       _ledgerPosting;
    private readonly ILogger<RefundAllocationService>    _logger;

    public RefundAllocationService(
        IPaymentEconomicsSnapshotRepository snapshots,
        IRefundAllocationPolicyRepository   policies,
        IRefundAllocationRepository         allocations,
        IProviderBalanceRepository          balances,
        FinancialLedgerPostingService       ledgerPosting,
        ILogger<RefundAllocationService>    logger)
    {
        _snapshots     = snapshots;
        _policies      = policies;
        _allocations   = allocations;
        _balances      = balances;
        _ledgerPosting = ledgerPosting;
        _logger        = logger;
    }

    /// <summary>
    /// Allocates the refund from the transaction's economics snapshot, persists the 9-amount breakdown, runs the §7.3
    /// recovery for a release-after refund (clawback → provider negative-balance ledger), and marks the benefit restore
    /// once. Returns the computed allocation (or null when the transaction has no linked snapshot — legacy path).
    /// <para><paramref name="requestedRefundAmount"/> is the customer-facing gateway refund total (service + platform
    /// fee). The service portion is derived proportionally from the immutable snapshot (§7.5) — never recomputed from
    /// rates. A full refund (≥ CustomerTotal) maps to the exact snapshot service amount.</para>
    /// </summary>
    public async Task<RefundAllocation?> ApplyAsync(
        PaymentTransactionEntity tx, TransactionRefundRecord record, decimal requestedRefundAmount,
        RefundCause cause, bool restoreBenefit, CancellationToken ct)
    {
        if (tx.EconomicsSnapshotId is not { } snapId) return null;   // no snapshot → no allocation (legacy)
        var snapshot = await _snapshots.GetByIdAsync(snapId, ct);
        if (snapshot is null) return null;

        // Derive the service portion of the requested gateway refund from the immutable snapshot (§7.5 — proportional,
        // never rate-recomputed). Full refund (≥ CustomerTotal) → exact snapshot service amount.
        var serviceAmount    = MoneyMath.Round(snapshot.ServiceAmountSnapshot);
        var customerTotal    = MoneyMath.Round(snapshot.CustomerTotalAmountSnapshot);
        var refundServiceAmount =
            customerTotal <= 0m || requestedRefundAmount >= customerTotal
                ? serviceAmount
                : MoneyMath.Round(serviceAmount * (MoneyMath.Round(requestedRefundAmount) / customerTotal));

        var now          = DateTime.UtcNow;
        var releaseState = tx.ReleasedAt.HasValue ? ReleaseState.AfterProviderRelease : ReleaseState.BeforeProviderRelease;

        var policy = await _policies.ResolveAsync(tx.CurrencyCode, now, ct);
        var (mode, fixedFee) = policy?.RuleFor(cause) ?? (PlatformFeeRefundMode.Full, (decimal?)null);

        var allocation = RefundAllocationCalculator.Resolve(
            snapshot, refundServiceAmount, cause, releaseState, mode, fixedFee);

        // §7.3 release-after recovery: clawback the provider's net reversal into the negative-balance ledger; the platform
        // fronts (advances) that amount to the customer immediately (a receivable auto-offset from future payouts).
        if (releaseState == ReleaseState.AfterProviderRelease && allocation.ProviderNetReversalAmount > 0m && tx.RecipientProfileId is { } providerId)
        {
            var balance = await _balances.GetByProviderAsync(providerId, tx.CurrencyCode, ct);
            var isNewBalance = balance is null;
            if (balance is null)
            {
                balance = ProviderBalanceEntity.Create(providerId, tx.CurrencyCode, policy?.NegativeBalanceLimit ?? 0m);
                await _balances.AddAsync(balance, ct);
            }
            balance.Clawback(allocation.ProviderNetReversalAmount, ProviderBalanceMovementType.RefundClawback,
                refundRecordId: record.Id == 0 ? null : record.Id, chargebackRecordId: null, note: $"Refund {record.RefundCode}", now);
            if (!isNewBalance) _balances.Update(balance);   // a freshly-added balance is already tracked as Added

            allocation = allocation with
            {
                PlatformAdvancedRefundAmount     = allocation.ProviderNetReversalAmount,
                RemainingProviderNegativeBalance = balance.NegativeAmount,
            };
        }

        // Persist the record (to materialise its Id) then the immutable allocation, and link them.
        await _allocations.SaveChangesAsync(ct);   // flush the pending refund record → record.Id
        var entity = RefundAllocationEntity.Create(record.Id, snapId, cause, releaseState, tx.CurrencyCode, allocation);
        await _allocations.AddAsync(entity, ct);
        await _allocations.SaveChangesAsync(ct);   // → entity.Id
        record.SetAllocation(cause, releaseState, entity.Id);

        // ── BE-P12: post the refund reporting ledger from the immutable allocation (reversal + refund/recovery lines). ──
        await _ledgerPosting.PostRefundAsync(tx, record.Id, entity, ct);

        // §19.15 benefit restore runs ONCE per refund (idempotent — a duplicate refund/webhook throws).
        if (restoreBenefit)
        {
            record.MarkBenefitRestoreApplied();
            // The platform-funded customer benefit budget + P7 entitlement re-credit (per RefundRestorePolicy) is located
            // by the offer contextRef when a per-customer budget exists (dormant in the narrow core; additive seam).
            _logger.LogInformation("Refund {Code}: benefit-restore marked (once).", record.RefundCode);
        }

        _logger.LogInformation(
            "Refund allocation {Code}: service {Svc} (providerNet {Net} + commission {Comm}), fee gross {Fee}, total {Total}, release {State}, advanced {Adv}.",
            record.RefundCode, allocation.ServiceRefundAmount, allocation.ProviderNetReversalAmount,
            allocation.CommissionRevenueReversalAmount, allocation.PlatformFeeGrossRefundAmount,
            allocation.TotalRefundToGateway, releaseState, allocation.PlatformAdvancedRefundAmount);

        return allocation;
    }
}
