using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing;

/// <summary>S2a — an admin pricing attribute definition + its category scope.</summary>
public sealed class PricingAttributeDefinitionDto
{
    public long                     Id              { get; set; }
    public string                   Code            { get; set; } = default!;
    public string                   NameTr          { get; set; } = default!;
    public string                   NameEn          { get; set; } = default!;
    public PricingAttributeDataType DataType        { get; set; }
    public string?                  LookupGroupCode { get; set; }
    public bool                     IsRequired      { get; set; }
    public int                      SortOrder       { get; set; }
    public decimal?                 MinValue        { get; set; }
    public decimal?                 MaxValue        { get; set; }
    public bool                     IsActive        { get; set; }
    public List<string>             ServiceCategoryCodes { get; set; } = new();
}
