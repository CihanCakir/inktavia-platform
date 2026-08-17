using Aizen.Core.Domain;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Domain.Entities;

/// <summary>
/// Records the outcome of processing a single settlement within a
/// <see cref="CargoDrySettlementAutomationRunEntity"/>.
/// One item is created per settlement in scope, including skipped and errored settlements.
/// Phase 6 (July 2026).
/// </summary>
[DocumentationInfo("CargoDry Settlement Automation Run Item",
    "Records the per-settlement result of a monthly automation run. " +
    "One item per settlement in scope. Captures action taken, success/failure, " +
    "attribution counters, and any error message. " +
    "Phase 6 (July 2026).")]
public sealed class CargoDrySettlementAutomationRunItemEntity : AizenEntity
{
    // ── FK to parent run ──────────────────────────────────────────────────────────
    /// <summary>FK to CargoDrySettlementAutomationRunEntity.</summary>
    public long RunId { get; private set; }

    // ── Settlement reference (cross-module, denormalized for display) ─────────────
    /// <summary>Cross-module reference to CargoDrySellThroughSettlementEntity. No EF FK.</summary>
    public long   SettlementId   { get; private set; }

    /// <summary>Denormalized settlement code for display (e.g. "STS-2-TRY-CD1-202606").</summary>
    public string SettlementCode { get; private set; } = default!;

    /// <summary>Denormalized provider profile id for reporting.</summary>
    public long   ProviderProfileId { get; private set; }

    /// <summary>Denormalized product code for reporting.</summary>
    public string ProductCode { get; private set; } = default!;

    /// <summary>Denormalized ISO 4217 currency code for reporting.</summary>
    public string CurrencyCode { get; private set; } = default!;

    /// <summary>Denormalized period year-month (YYYYMM) for reporting.</summary>
    public int PeriodYearMonth { get; private set; }

    // ── State before processing ───────────────────────────────────────────────────
    /// <summary>Settlement status at the time this item was evaluated.</summary>
    public CargoDrySellThroughSettlementStatus StatusBefore { get; private set; }

    // ── Outcome ───────────────────────────────────────────────────────────────────
    /// <summary>
    /// Human-readable action taken or reason for skipping.
    /// Examples: "MarkedReadyForSettlement", "PaymentPrepared", "Skipped:AlreadyReadyForSettlement",
    ///           "Skipped:UnresolvedAttributions", "Error:CommandHandlerException", "DryRun:WouldMarkReadyForSettlement"
    /// </summary>
    public string Action { get; private set; } = default!;

    /// <summary>True if the action completed without error (or DryRun analysis succeeded).</summary>
    public bool Success { get; private set; }

    /// <summary>Error message if Success = false.</summary>
    public string? ErrorMessage { get; private set; }

    // ── Attribution counters ──────────────────────────────────────────────────────
    /// <summary>Number of attributions that were financially resolved during this run.</summary>
    public int AttributionsResolved { get; private set; }

    /// <summary>Number of attributions skipped (already resolved, or DryRun mode).</summary>
    public int AttributionsSkipped { get; private set; }

    /// <summary>Number of attributions that errored during resolution attempt.</summary>
    public int AttributionsErrored { get; private set; }

    // ── Timestamps ────────────────────────────────────────────────────────────────
    public DateTime ProcessedAtUtc { get; private set; }

    private CargoDrySettlementAutomationRunItemEntity() { }

    // ── Factory ───────────────────────────────────────────────────────────────────
    public static CargoDrySettlementAutomationRunItemEntity Create(
        long                               runId,
        long                               settlementId,
        string                             settlementCode,
        long                               providerProfileId,
        string                             productCode,
        string                             currencyCode,
        int                                periodYearMonth,
        CargoDrySellThroughSettlementStatus statusBefore,
        string                             action,
        bool                               success,
        string?                            errorMessage,
        int                                attributionsResolved,
        int                                attributionsSkipped,
        int                                attributionsErrored,
        DateTime                           processedAtUtc)
    {
        return new CargoDrySettlementAutomationRunItemEntity
        {
            RunId                = runId,
            SettlementId         = settlementId,
            SettlementCode       = settlementCode,
            ProviderProfileId    = providerProfileId,
            ProductCode          = productCode,
            CurrencyCode         = currencyCode,
            PeriodYearMonth      = periodYearMonth,
            StatusBefore         = statusBefore,
            Action               = action,
            Success              = success,
            ErrorMessage         = errorMessage,
            AttributionsResolved = attributionsResolved,
            AttributionsSkipped  = attributionsSkipped,
            AttributionsErrored  = attributionsErrored,
            ProcessedAtUtc       = processedAtUtc,
        };
    }
}
