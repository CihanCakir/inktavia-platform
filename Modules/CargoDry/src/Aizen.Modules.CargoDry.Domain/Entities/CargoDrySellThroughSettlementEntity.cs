using Aizen.Core.Domain;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Domain.Entities;

[DocumentationInfo("CargoDry Sell-Through Settlement entity",
    "Groups multiple ConsignmentSellThrough sales attribution records for one consignment " +
    "agreement period into a single settlement batch ready for provider payout. " +
    "Created by ICargoDryCommercialActivationService when the first ConsignmentSellThrough " +
    "kit in a period is activated (or when an existing open settlement is found for the period). " +
    "Phase 3 (July 2026): Sales Attribution & Sell-Through Settlement Foundation.")]
public sealed class CargoDrySellThroughSettlementEntity : AizenEntityWithAudit
{
    // ── Identity ────────────────────────────────────────────────────────────────
    /// <summary>Human-readable unique code, e.g. "STS-2026-07-001". Generated at creation.</summary>
    public string  SettlementCode { get; private set; } = default!;

    // ── Scope ───────────────────────────────────────────────────────────────────
    /// <summary>FK to CargoDryConsignmentAgreementEntity. Cross-module reference; no EF FK constraint.</summary>
    public long    ConsignmentAgreementId { get; private set; }

    /// <summary>Cross-module reference to Identity/Profile. No EF FK constraint.</summary>
    public long    ProviderProfileId      { get; private set; }

    public string  ProductCode { get; private set; } = default!;

    /// <summary>Null when the settlement spans multiple batches under the same agreement.</summary>
    public string? BatchCode   { get; private set; }

    // ── Kit counts ──────────────────────────────────────────────────────────────
    /// <summary>Total kits in scope for this settlement period.</summary>
    public int TotalKitCount   { get; private set; }

    /// <summary>Kits for which payout has been confirmed. Incremented when attribution is marked Settled.</summary>
    public int SettledKitCount { get; private set; }

    // ── Financials ──────────────────────────────────────────────────────────────
    public decimal TotalSaleAmount       { get; private set; }
    public decimal TotalCommissionAmount { get; private set; }

    /// <summary>Amount to be paid out to the provider: TotalSaleAmount - TotalCommissionAmount.</summary>
    public decimal ProviderPayoutAmount  { get; private set; }

    /// <summary>ISO 4217 currency code for all amounts.</summary>
    public string  CurrencyCode          { get; private set; } = default!;

    // ── Period ──────────────────────────────────────────────────────────────────
    public DateTime PeriodStartUtc { get; private set; }
    public DateTime PeriodEndUtc   { get; private set; }

    // ── Status ──────────────────────────────────────────────────────────────────
    public CargoDrySellThroughSettlementStatus Status { get; private set; }

    // ── Settlement audit ────────────────────────────────────────────────────────
    /// <summary>Date finance has scheduled for the provider payout transfer.</summary>
    public DateTime? ScheduledSettlementDate { get; private set; }
    public DateTime? SettledAtUtc            { get; private set; }
    public long?     SettledByUserId         { get; private set; }

    /// <summary>Provider-supplied dispute reason when Status = Disputed.</summary>
    public string?   DisputeReason           { get; private set; }

    /// <summary>Admin note (resolution note, scheduling note, etc.).</summary>
    public string?   Note                    { get; private set; }

    public DateTime  CreatedAtUtc            { get; private set; }

    private CargoDrySellThroughSettlementEntity() { }

    // ── Factory ─────────────────────────────────────────────────────────────────
    public static CargoDrySellThroughSettlementEntity Create(
        string   settlementCode,
        long     consignmentAgreementId,
        long     providerProfileId,
        string   productCode,
        string?  batchCode,
        string   currencyCode,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        DateTime nowUtc)
    {
        if (periodEndUtc <= periodStartUtc)
            throw new ArgumentException(
                "PeriodEndUtc must be after PeriodStartUtc.", nameof(periodEndUtc));

        return new CargoDrySellThroughSettlementEntity
        {
            SettlementCode         = settlementCode,
            ConsignmentAgreementId = consignmentAgreementId,
            ProviderProfileId      = providerProfileId,
            ProductCode            = productCode,
            BatchCode              = batchCode,
            CurrencyCode           = currencyCode,
            PeriodStartUtc         = periodStartUtc,
            PeriodEndUtc           = periodEndUtc,
            TotalKitCount          = 0,
            SettledKitCount        = 0,
            TotalSaleAmount        = 0m,
            TotalCommissionAmount  = 0m,
            ProviderPayoutAmount   = 0m,
            Status                 = CargoDrySellThroughSettlementStatus.Pending,
            CreatedAtUtc           = nowUtc,
            IsActive               = true,
        };
    }

    // ── Domain mutation methods ──────────────────────────────────────────────────

    /// <summary>
    /// Adds one attribution's financials to this settlement.
    /// Called when a new ConsignmentSellThrough kit activation is attributed to this settlement.
    /// </summary>
    public void AddAttribution(decimal salePrice, decimal commissionAmount, DateTime nowUtc)
    {
        if (Status != CargoDrySellThroughSettlementStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot add attribution to settlement {Id} with status {Status}.");

        TotalKitCount         += 1;
        TotalSaleAmount       += salePrice;
        TotalCommissionAmount += commissionAmount;
        ProviderPayoutAmount   = TotalSaleAmount - TotalCommissionAmount;
    }

    /// <summary>
    /// Finance marks the settlement as ready for payout after reviewing amounts.
    /// </summary>
    public void MarkReadyForSettlement(string? note = null)
    {
        if (Status != CargoDrySellThroughSettlementStatus.Pending)
            throw new InvalidOperationException(
                $"Settlement {Id} must be in Pending status to mark ReadyForSettlement.");

        Status = CargoDrySellThroughSettlementStatus.ReadyForSettlement;
        if (note is not null) Note = note;
    }

    /// <summary>Finance schedules the payout for a specific date.</summary>
    public void Schedule(DateTime scheduledDate, string? note = null)
    {
        if (Status != CargoDrySellThroughSettlementStatus.ReadyForSettlement)
            throw new InvalidOperationException(
                $"Settlement {Id} must be ReadyForSettlement to schedule.");

        ScheduledSettlementDate = scheduledDate;
        Status                  = CargoDrySellThroughSettlementStatus.Scheduled;
        if (note is not null) Note = note;
    }

    /// <summary>Finance confirms the provider payout has been executed.</summary>
    public void MarkSettled(long settledByUserId, DateTime nowUtc, string? note = null)
    {
        if (Status != CargoDrySellThroughSettlementStatus.Scheduled)
            throw new InvalidOperationException(
                $"Settlement {Id} must be Scheduled before marking Settled.");

        SettledAtUtc    = nowUtc;
        SettledByUserId = settledByUserId;
        Status          = CargoDrySellThroughSettlementStatus.Settled;
        if (note is not null) Note = note;
    }

    /// <summary>Provider raises a dispute on the settlement amounts.</summary>
    public void RaiseDispute(string disputeReason)
    {
        if (Status is CargoDrySellThroughSettlementStatus.Settled
                   or CargoDrySellThroughSettlementStatus.Cancelled
                   or CargoDrySellThroughSettlementStatus.Disputed)
            throw new InvalidOperationException(
                $"Cannot raise dispute on settlement {Id} with status {Status}.");

        DisputeReason = disputeReason;
        Status        = CargoDrySellThroughSettlementStatus.Disputed;
    }

    /// <summary>Admin cancels the settlement.</summary>
    public void Cancel(string? reason = null)
    {
        if (Status is CargoDrySellThroughSettlementStatus.Settled)
            throw new InvalidOperationException(
                $"Cannot cancel already-settled settlement {Id}.");

        Status = CargoDrySellThroughSettlementStatus.Cancelled;
        if (reason is not null) Note = reason;
    }
}
