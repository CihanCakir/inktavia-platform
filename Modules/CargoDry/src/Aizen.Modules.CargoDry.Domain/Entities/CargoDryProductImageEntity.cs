using Aizen.Core.Domain;

namespace Aizen.Modules.CargoDry.Domain.Entities;

[DocumentationInfo("CargoDry Product Image entity",
    "An ordered gallery image for a CargoDry product. Stores the FileStorage FileId only; the presigned display " +
    "URL is resolved at the BFF boundary (admin + mobile) via IFileStorageRemoteCall.CreateReadUrl. Additive " +
    "(CargoDry supply flow) — real photos are uploaded by admin later; no seed images.")]
public sealed class CargoDryProductImageEntity : AizenEntityWithAudit
{
    /// <summary>FK to the owning <see cref="CargoDryProductEntity"/>.</summary>
    public long ProductId { get; private set; }

    /// <summary>FileStorage file id (OwnerModule=CargoDry). Resolved to a read URL at the BFF layer.</summary>
    public Guid FileId { get; private set; }

    /// <summary>Zero-based display order within the product gallery.</summary>
    public int SortOrder { get; private set; }

    private CargoDryProductImageEntity() { }

    public static CargoDryProductImageEntity Create(long productId, Guid fileId, int sortOrder)
        => new()
        {
            ProductId = productId,
            FileId    = fileId,
            SortOrder = sortOrder,
            IsActive  = true,
        };

    public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;
}
