using Aizen.Core.Domain;

namespace Aizen.Modules.CargoDry.Domain.Entities;

[DocumentationInfo("CargoDry Batch entity",
    "A production batch groups a set of generated kit serial numbers. Each batch has its own " +
    "HMAC secret key stored in Key Vault. Revoking a batch invalidates all un-activated kits in it.")]
public sealed class CargoDryBatchEntity : AizenEntityWithAudit
{
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
}
