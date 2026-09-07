using Aizen.Core.Domain;

namespace Aizen.Modules.ReferenceData.Domain.Entities.Catalog;

[DocumentationInfo("Engine brand", "Engine manufacturer/brand reference for the vessel wizard.")]
public sealed class EngineBrandEntity : AizenEntityWithAudit
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public bool NeedsReview { get; private set; }
    public string? Source { get; private set; }

    public EngineBrandEntity() { }

    public static EngineBrandEntity Create(string code, string name, bool needsReview, string? source)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Brand code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Brand name is required.", nameof(name));
        return new EngineBrandEntity
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = CatalogNameNormalizer.Clean(name),
            NeedsReview = needsReview,
            Source = source?.Trim(),
            IsActive = true,
        };
    }

    public void Update(string name, bool isActive)
    {
        Name = CatalogNameNormalizer.Clean(name);
        IsActive = isActive;
    }

    public void Approve() => NeedsReview = false;
    public void Activate() => IsActive = true;
    /// <summary>Set when this row was merged into another (audit). Non-null ⇒ deactivated duplicate.</summary>
    public long? MergedIntoId { get; private set; }

    public void Deactivate() => IsActive = false;

    /// <summary>Merge audit: record the surviving target id and deactivate this duplicate.</summary>
    public void MarkMergedInto(long targetId)
    {
        MergedIntoId = targetId;
        IsActive = false;
    }
}
