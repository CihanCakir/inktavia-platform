using Aizen.Core.Domain;

namespace Aizen.Modules.CargoDry.Domain.Entities;

public sealed class CargoDryBatchEntity : AizenEntity
{
    public string  BatchCode       { get; private set; } = default!;
    public string  ProductCode     { get; private set; } = default!;
    public int     KitCount        { get; private set; }
    public bool    IsRevoked       { get; private set; }
    public string? RevokeReason    { get; private set; }
    public string? QrZipFileRef    { get; private set; }
    public string? ExcelFileRef    { get; private set; }
    public DateTimeOffset CreatedAt  { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public long CreatedByAdminId   { get; private set; }

    private CargoDryBatchEntity() { }

    public static CargoDryBatchEntity Create(
        string batchCode, string productCode, int kitCount, long adminId)
        => new()
        {
            BatchCode         = batchCode,
            ProductCode       = productCode,
            KitCount          = kitCount,
            IsRevoked         = false,
            CreatedAt         = DateTimeOffset.UtcNow,
            CreatedByAdminId  = adminId,
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
