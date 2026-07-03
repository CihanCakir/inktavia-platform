using Aizen.Core.Domain;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Domain.Entities;

[DocumentationInfo("CargoDry Inventory Movement entity",
    "Immutable ledger row for every stock change in provider inventory. " +
    "Positive quantity = stock in; negative = stock out. " +
    "Rows are never updated or deleted — only appended. " +
    "Phase 2 (July 2026): Created as part of CargoDry provider inventory foundation.")]
public sealed class CargoDryInventoryMovementEntity : AizenEntityWithAudit
{
    // ── Context ────────────────────────────────────────────────────────────────
    /// <summary>Cross-module reference to Identity/Profile. No EF FK constraint.</summary>
    public long    ProviderProfileId { get; private set; }

    public string  ProductCode       { get; private set; } = default!;

    /// <summary>Nullable — set for batch-level movements; null for kit-level or manual.</summary>
    public string? BatchCode         { get; private set; }

    /// <summary>Nullable — set for kit-level movements (activate/revoke/return/transfer).</summary>
    public long?   KitId             { get; private set; }

    // ── Movement ───────────────────────────────────────────────────────────────
    public InventoryMovementType     MovementType    { get; private set; }

    /// <summary>
    /// Positive for stock-in (BatchAllocated, ManualAdjustment positive).
    /// Negative for stock-out (KitActivated, KitRevoked, KitReturned, ManualAdjustment negative).
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>AvailableStock after this movement was applied. Null if not computed at creation time.</summary>
    public int? BalanceAfter { get; private set; }

    // ── Commercial context ─────────────────────────────────────────────────────
    public CargoDryCommercialModel? CommercialModel { get; private set; }
    public SalesChannel?            SalesChannel    { get; private set; }

    // ── Reference ─────────────────────────────────────────────────────────────
    /// <summary>Type of the originating record (e.g. "ConsignmentAgreement", "Batch", "Kit").</summary>
    public string? ReferenceType { get; private set; }

    /// <summary>Id of the originating record (cross-module, Id-only reference).</summary>
    public long?   ReferenceId   { get; private set; }

    /// <summary>Free-text note explaining the movement (especially for ManualAdjustment).</summary>
    public string? Note          { get; private set; }

    // ── Audit ──────────────────────────────────────────────────────────────────
    public DateTime CreatedAtUtc      { get; private set; }

    /// <summary>Admin or system user who initiated the movement.</summary>
    public long?    CreatedByUserId   { get; private set; }

    private CargoDryInventoryMovementEntity() { }

    // ── Factory ────────────────────────────────────────────────────────────────
    public static CargoDryInventoryMovementEntity Create(
        long                    providerProfileId,
        string                  productCode,
        InventoryMovementType   movementType,
        int                     quantity,
        DateTime                nowUtc,
        string?                 batchCode       = null,
        long?                   kitId           = null,
        int?                    balanceAfter    = null,
        CargoDryCommercialModel? commercialModel = null,
        SalesChannel?           salesChannel    = null,
        string?                 referenceType   = null,
        long?                   referenceId     = null,
        string?                 note            = null,
        long?                   createdByUserId = null)
    {
        if (quantity == 0)
            throw new ArgumentException("Movement quantity must be non-zero.", nameof(quantity));

        return new()
        {
            ProviderProfileId = providerProfileId,
            ProductCode       = productCode,
            BatchCode         = batchCode,
            KitId             = kitId,
            MovementType      = movementType,
            Quantity          = quantity,
            BalanceAfter      = balanceAfter,
            CommercialModel   = commercialModel,
            SalesChannel      = salesChannel,
            ReferenceType     = referenceType,
            ReferenceId       = referenceId,
            Note              = note,
            CreatedAtUtc      = nowUtc,
            CreatedByUserId   = createdByUserId,
            IsActive          = true,
        };
    }
}
