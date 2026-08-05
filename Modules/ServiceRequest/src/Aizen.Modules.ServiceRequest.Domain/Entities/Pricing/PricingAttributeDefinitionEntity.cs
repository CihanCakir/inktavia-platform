using Aizen.Core.Domain;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.Pricing;

/// <summary>
/// S2a (§20.6) — an admin-owned, category-scoped <b>pricing attribute definition</b>: the schema for a pricing-relevant
/// variable a provider records on an offer line (e.g. EngineInstallationType → R4 <c>ENGINE_INSTALLATION_TYPE</c>,
/// PaintType → <c>PAINT_TYPE</c>, WorkDifficulty → <c>WORK_DIFFICULTY</c>). Descriptive only — it never enters the line
/// money math. <see cref="Code"/> is stable/unique; a <see cref="PricingAttributeDefinitionCategoryEntity"/> join scopes
/// the definition to the ServiceRequest categories it applies to. When <see cref="DataType"/> is
/// <see cref="PricingAttributeDataType.Lookup"/>, <see cref="LookupGroupCode"/> is the R4 group the value must belong to
/// (resolved fail-loud via the ReferenceData remote call). <see cref="MinValue"/>/<see cref="MaxValue"/> bound Number
/// attributes.
/// </summary>
public sealed class PricingAttributeDefinitionEntity : AizenEntityWithAudit
{
    public string                   Code            { get; private set; } = default!;
    public string                   NameTr          { get; private set; } = default!;
    public string                   NameEn          { get; private set; } = default!;
    public PricingAttributeDataType DataType        { get; private set; }
    /// <summary>Required (and only meaningful) when <see cref="DataType"/> = Lookup — the R4 marine group code.</summary>
    public string?                  LookupGroupCode { get; private set; }
    public bool                     IsRequired      { get; private set; }
    public int                      SortOrder       { get; private set; }
    /// <summary>Number bounds (inclusive). Only meaningful for <see cref="PricingAttributeDataType.Number"/>.</summary>
    public decimal?                 MinValue        { get; private set; }
    public decimal?                 MaxValue        { get; private set; }

    private readonly List<PricingAttributeDefinitionCategoryEntity> _categories = new();
    public IReadOnlyCollection<PricingAttributeDefinitionCategoryEntity> Categories => _categories.AsReadOnly();

    private PricingAttributeDefinitionEntity() { }

    public static PricingAttributeDefinitionEntity Create(
        string code, string nameTr, string nameEn, PricingAttributeDataType dataType,
        string? lookupGroupCode, bool isRequired, int sortOrder,
        decimal? minValue, decimal? maxValue, IEnumerable<string> serviceCategoryCodes)
    {
        var entity = new PricingAttributeDefinitionEntity
        {
            Code            = code.Trim().ToUpperInvariant(),
            NameTr          = nameTr.Trim(),
            NameEn          = nameEn.Trim(),
            DataType        = dataType,
            LookupGroupCode = dataType == PricingAttributeDataType.Lookup ? lookupGroupCode?.Trim().ToUpperInvariant() : null,
            IsRequired      = isRequired,
            SortOrder       = sortOrder,
            MinValue        = dataType == PricingAttributeDataType.Number ? minValue : null,
            MaxValue        = dataType == PricingAttributeDataType.Number ? maxValue : null,
            IsActive        = true,
        };
        entity.ReplaceCategories(serviceCategoryCodes);
        return entity;
    }

    public void Update(
        string nameTr, string nameEn, PricingAttributeDataType dataType,
        string? lookupGroupCode, bool isRequired, int sortOrder,
        decimal? minValue, decimal? maxValue, IEnumerable<string> serviceCategoryCodes)
    {
        // Code is immutable (stable key). DataType/group/bounds and scope may change.
        NameTr          = nameTr.Trim();
        NameEn          = nameEn.Trim();
        DataType        = dataType;
        LookupGroupCode = dataType == PricingAttributeDataType.Lookup ? lookupGroupCode?.Trim().ToUpperInvariant() : null;
        IsRequired      = isRequired;
        SortOrder       = sortOrder;
        MinValue        = dataType == PricingAttributeDataType.Number ? minValue : null;
        MaxValue        = dataType == PricingAttributeDataType.Number ? maxValue : null;
        ReplaceCategories(serviceCategoryCodes);
    }

    public void Deactivate() => IsActive = false;

    public void ReplaceCategories(IEnumerable<string> serviceCategoryCodes)
    {
        _categories.Clear();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in serviceCategoryCodes ?? Enumerable.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var code = raw.Trim().ToUpperInvariant();
            if (seen.Add(code))
                _categories.Add(PricingAttributeDefinitionCategoryEntity.Create(code));
        }
    }
}
