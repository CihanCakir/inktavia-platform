using Aizen.Core.Domain;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Domain.Entities;

[DocumentationInfo("CargoDry Consignment Agreement entity",
    "Defines the commercial terms under which Inktavia sends CargoDry stock to a provider " +
    "without treating the stock transfer as an immediate sale. " +
    "Default model is ConsignmentSellThrough (Decision N3). " +
    "Phase 1 (July 2026): Created as part of CargoDry commercial foundation.")]
public sealed class CargoDryConsignmentAgreementEntity : AizenEntityWithAudit
{
    // ── Core identity ──────────────────────────────────────────────────────────
    /// <summary>Human-readable unique code (e.g. "CSG-2026-PROVIDER-001"). Admin-assigned.</summary>
    public string AgreementCode { get; private set; } = default!;

    /// <summary>Provider this agreement is with. Cross-module Id only — no EF FK to Identity.</summary>
    public long ProviderProfileId { get; private set; }

    /// <summary>Which product this consignment covers. One agreement per provider+product is active at a time.</summary>
    public string ProductCode { get; private set; } = default!;

    // ── Commercial terms ───────────────────────────────────────────────────────
    /// <summary>
    /// Fraction of retail price the provider earns on each sell-through (0.0–1.0).
    /// E.g. 0.25 = provider keeps 25%, Inktavia keeps 75%.
    /// </summary>
    public decimal ConsignmentRate { get; private set; }

    /// <summary>
    /// Minimum cumulative sell-through amount before settlement is triggered in a billing cycle.
    /// 0 = settle any amount.
    /// </summary>
    public decimal MinimumSettlementAmount { get; private set; }

    /// <summary>Currency for all monetary values in this agreement.</summary>
    public string CurrencyCode { get; private set; } = "TRY";

    /// <summary>Maximum number of kits that can be allocated to the provider under this agreement.</summary>
    public int MaxKitCount { get; private set; }

    /// <summary>Running count of kits already allocated to this provider under this agreement.</summary>
    public int AllocatedKitCount { get; private set; }

    // ── Status & lifecycle ─────────────────────────────────────────────────────
    public ConsignmentAgreementStatus Status { get; private set; }

    /// <summary>Agreement starts on this date (UTC). Allocation is only valid after this date.</summary>
    public DateTime StartDateUtc { get; private set; }

    /// <summary>Optional end date. After this date the agreement expires. Null = open-ended.</summary>
    public DateTime? EndDateUtc { get; private set; }

    // ── Optional metadata ──────────────────────────────────────────────────────
    /// <summary>Reference to a stored terms document (e.g. PDF ref, object storage key).</summary>
    public string? TermsDocumentRef { get; private set; }

    /// <summary>Internal notes visible only to admin.</summary>
    public string? Notes { get; private set; }

    // ── Status timestamps ──────────────────────────────────────────────────────
    public DateTime? ActivatedAtUtc   { get; private set; }
    public DateTime? SuspendedAtUtc   { get; private set; }
    public DateTime? TerminatedAtUtc  { get; private set; }

    /// <summary>Reason recorded when the agreement is suspended.</summary>
    public string? SuspendReason { get; private set; }

    /// <summary>Reason recorded when the agreement is terminated.</summary>
    public string? TerminationReason { get; private set; }

    private CargoDryConsignmentAgreementEntity() { }

    // ── Factory ────────────────────────────────────────────────────────────────
    public static CargoDryConsignmentAgreementEntity Create(
        string  agreementCode,
        long    providerProfileId,
        string  productCode,
        decimal consignmentRate,
        decimal minimumSettlementAmount,
        string  currencyCode,
        int     maxKitCount,
        DateTime startDateUtc,
        DateTime? endDateUtc            = null,
        string? termsDocumentRef        = null,
        string? notes                   = null)
        => new()
        {
            AgreementCode           = agreementCode,
            ProviderProfileId       = providerProfileId,
            ProductCode             = productCode,
            ConsignmentRate         = consignmentRate,
            MinimumSettlementAmount = minimumSettlementAmount,
            CurrencyCode            = currencyCode,
            MaxKitCount             = maxKitCount,
            AllocatedKitCount       = 0,
            Status                  = ConsignmentAgreementStatus.Draft,
            StartDateUtc            = DateTime.SpecifyKind(startDateUtc, DateTimeKind.Utc),
            EndDateUtc              = endDateUtc.HasValue
                                        ? DateTime.SpecifyKind(endDateUtc.Value, DateTimeKind.Utc)
                                        : null,
            TermsDocumentRef        = termsDocumentRef,
            Notes                   = notes,
        };

    // ── Lifecycle domain methods ───────────────────────────────────────────────

    /// <summary>
    /// Activates the agreement. Only Draft agreements can be activated.
    /// </summary>
    public void Activate()
    {
        if (Status != ConsignmentAgreementStatus.Draft)
            throw new InvalidOperationException(
                $"Agreement '{AgreementCode}' cannot be activated — current status: {Status}. Only Draft agreements can be activated.");

        Status          = ConsignmentAgreementStatus.Active;
        ActivatedAtUtc  = DateTime.UtcNow;
    }

    /// <summary>
    /// Suspends an Active agreement. Allocation pauses; existing kits remain with the provider.
    /// </summary>
    public void Suspend(string reason)
    {
        if (Status != ConsignmentAgreementStatus.Active)
            throw new InvalidOperationException(
                $"Agreement '{AgreementCode}' cannot be suspended — current status: {Status}. Only Active agreements can be suspended.");

        Status          = ConsignmentAgreementStatus.Suspended;
        SuspendReason   = reason;
        SuspendedAtUtc  = DateTime.UtcNow;
    }

    /// <summary>
    /// Terminates an Active or Suspended agreement. Terminal state — no further transitions.
    /// </summary>
    public void Terminate(string reason)
    {
        if (Status != ConsignmentAgreementStatus.Active && Status != ConsignmentAgreementStatus.Suspended)
            throw new InvalidOperationException(
                $"Agreement '{AgreementCode}' cannot be terminated — current status: {Status}. Only Active or Suspended agreements can be terminated.");

        Status             = ConsignmentAgreementStatus.Terminated;
        TerminationReason  = reason;
        TerminatedAtUtc    = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the agreement as expired. Called by background job when EndDateUtc has passed.
    /// </summary>
    public void MarkExpired()
    {
        if (Status == ConsignmentAgreementStatus.Active)
            Status = ConsignmentAgreementStatus.Expired;
    }

    /// <summary>
    /// Updates editable terms. Only Draft or Suspended agreements can be updated.
    /// </summary>
    public void UpdateTerms(
        decimal   consignmentRate,
        decimal   minimumSettlementAmount,
        string    currencyCode,
        int       maxKitCount,
        DateTime  startDateUtc,
        DateTime? endDateUtc,
        string?   termsDocumentRef,
        string?   notes)
    {
        if (Status == ConsignmentAgreementStatus.Active)
            throw new InvalidOperationException(
                $"Agreement '{AgreementCode}' is Active — suspend it before modifying commercial terms.");
        if (Status == ConsignmentAgreementStatus.Terminated || Status == ConsignmentAgreementStatus.Expired)
            throw new InvalidOperationException(
                $"Agreement '{AgreementCode}' is {Status} and cannot be modified.");

        ConsignmentRate         = consignmentRate;
        MinimumSettlementAmount = minimumSettlementAmount;
        CurrencyCode            = currencyCode;
        MaxKitCount             = maxKitCount;
        StartDateUtc            = DateTime.SpecifyKind(startDateUtc, DateTimeKind.Utc);
        EndDateUtc              = endDateUtc.HasValue
                                    ? DateTime.SpecifyKind(endDateUtc.Value, DateTimeKind.Utc)
                                    : null;
        TermsDocumentRef        = termsDocumentRef;
        Notes                   = notes;
    }

    /// <summary>
    /// Increments the allocated kit count when kits are sent to the provider.
    /// Validates against MaxKitCount.
    /// </summary>
    public void IncreaseAllocatedKitCount(int count)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Allocation count must be positive.");

        if (AllocatedKitCount + count > MaxKitCount)
            throw new InvalidOperationException(
                $"Cannot allocate {count} kit(s): agreement '{AgreementCode}' cap is {MaxKitCount}, " +
                $"already allocated {AllocatedKitCount}.");

        AllocatedKitCount += count;
    }

    // ── Guards ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if the given count can still be allocated without exceeding MaxKitCount.
    /// Also validates that the agreement is Active and within its validity period.
    /// </summary>
    public bool CanAllocate(int count, DateTime nowUtc)
    {
        if (Status != ConsignmentAgreementStatus.Active)
            return false;
        if (nowUtc < StartDateUtc)
            return false;
        if (EndDateUtc.HasValue && nowUtc > EndDateUtc.Value)
            return false;
        return AllocatedKitCount + count <= MaxKitCount;
    }

    /// <summary>
    /// Returns true if this agreement is Active and within its validity date range.
    /// </summary>
    public bool IsActive(DateTime nowUtc)
        => Status == ConsignmentAgreementStatus.Active
           && nowUtc >= StartDateUtc
           && (!EndDateUtc.HasValue || nowUtc <= EndDateUtc.Value);

    // ── Computed ───────────────────────────────────────────────────────────────

    /// <summary>Kits that can still be allocated under this agreement.</summary>
    public int RemainingKitCount => MaxKitCount - AllocatedKitCount;
}
