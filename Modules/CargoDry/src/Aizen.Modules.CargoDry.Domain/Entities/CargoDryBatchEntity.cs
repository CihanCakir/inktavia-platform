using Aizen.Core.Domain;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Domain.Entities;

[DocumentationInfo("CargoDry Batch entity",
    "A production batch groups a set of generated kit serial numbers. Each batch has its own " +
    "HMAC secret key stored in Key Vault. Revoking a batch invalidates all un-activated kits in it. " +
    "Phase 0 (July 2026): Added AssignedProviderProfileId, CommercialModel, ConsignmentAgreementId.")]
public sealed class CargoDryBatchEntity : AizenEntityWithAudit
{
    // ── Core fields ────────────────────────────────────────────────────────────
    public string  BatchCode        { get; private set; } = default!;
    public string  ProductCode      { get; private set; } = default!;
    public int     KitCount         { get; private set; }
    public bool    IsRevoked        { get; private set; }
    public string? RevokeReason     { get; private set; }
    public string? QrZipFileRef     { get; private set; }
    public string? ExcelFileRef     { get; private set; }
    /// <summary>Optional internal label set by admin at generation time (e.g. "Q3-REPLENISH-2024").</summary>
    public string? BatchLabel       { get; private set; }
    /// <summary>Optional warehouse/location code where kits are dispatched (e.g. "SGP-MAIN").</summary>
    public string? WarehouseCode    { get; private set; }
    /// <summary>Optional free-text production notes.</summary>
    public string? ProductionNotes  { get; private set; }
    // CreateDate from AizenEntityWithAudit — DO NOT re-declare
    public DateTimeOffset? RevokedAt         { get; private set; }
    public long            CreatedByAdminId  { get; private set; }

    // ── Commercial foundation (Phase 0, July 2026) ────────────────────────────
    /// <summary>
    /// Provider this batch is allocated to, if any.
    /// Null = batch stays in Inktavia platform warehouse (Decision N1/N2).
    /// </summary>
    public long? AssignedProviderProfileId { get; private set; }

    /// <summary>
    /// Commercial model that applies to all kits in this batch.
    /// Null until batch is allocated to a provider.
    /// Default when allocating to provider: ConsignmentSellThrough (Decision N3).
    /// </summary>
    public CargoDryCommercialModel? CommercialModel { get; private set; }

    /// <summary>
    /// FK to CargoDryConsignmentAgreementEntity (Phase 1 entity, not yet created).
    /// Null for DirectSale and ProviderResale batches.
    /// </summary>
    public long? ConsignmentAgreementId { get; private set; }

    private CargoDryBatchEntity() { }

    public static CargoDryBatchEntity Create(
        string batchCode, string productCode, int kitCount, long adminId,
        string? batchLabel = null, string? warehouseCode = null, string? productionNotes = null)
        => new()
        {
            BatchCode        = batchCode,
            ProductCode      = productCode,
            KitCount         = kitCount,
            IsRevoked        = false,
            CreatedByAdminId = adminId,
            IsActive         = true,
            BatchLabel       = batchLabel,
            WarehouseCode    = warehouseCode,
            ProductionNotes  = productionNotes,
        };

    public void Revoke(string reason)
    {
        IsRevoked    = true;
        RevokeReason = reason;
        RevokedAt    = DateTimeOffset.UtcNow;
    }

    public void SetFileRefs(string qrZipRef, string excelRef)
    {
        QrZipFileRef = qrZipRef;
        ExcelFileRef = excelRef;
    }

    /// <summary>
    /// Allocates this batch to a provider with a commercial model.
    /// Default model is ConsignmentSellThrough (Decision N3).
    /// </summary>
    public void AllocateToProvider(
        long providerProfileId,
        CargoDryCommercialModel model,
        long? consignmentAgreementId = null)
    {
        AssignedProviderProfileId = providerProfileId;
        CommercialModel           = model;
        ConsignmentAgreementId    = consignmentAgreementId;
    }
}
