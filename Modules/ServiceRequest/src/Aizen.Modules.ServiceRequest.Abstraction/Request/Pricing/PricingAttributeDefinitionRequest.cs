using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Pricing;

/// <summary>S2a — admin create/update payload for a pricing attribute definition.</summary>
public sealed class PricingAttributeDefinitionRequest
{
    public string                   Code            { get; set; } = default!;
    public string                   NameTr          { get; set; } = default!;
    public string                   NameEn          { get; set; } = default!;
    public PricingAttributeDataType DataType        { get; set; }
    public string?                  LookupGroupCode { get; set; }
    public bool                     IsRequired      { get; set; }
    public int                      SortOrder       { get; set; }
    public decimal?                 MinValue        { get; set; }
    public decimal?                 MaxValue        { get; set; }
    public List<string>             ServiceCategoryCodes { get; set; } = new();
}
