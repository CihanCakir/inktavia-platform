using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing;

/// <summary>
/// S2b/S2c — a pricing attribute applicable to an SR's category, with its Lookup options resolved (for the FE offer-line
/// picker). For non-Lookup definitions <see cref="Options"/> is empty.
/// </summary>
public sealed class ApplicablePricingAttributeDto
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
    public List<PricingAttributeOptionDto> Options  { get; set; } = new();
}

/// <summary>One Lookup option (R4 item): stable <see cref="Code"/> + tr/en labels.</summary>
public sealed class PricingAttributeOptionDto
{
    public string  Code   { get; set; } = default!;
    public string  NameTr { get; set; } = default!;
    public string? NameEn { get; set; }
}

/// <summary>S2b — a pricing attribute value currently set on an offer line.</summary>
public sealed class PricingAttributeValueDto
{
    public long     OfferItemId         { get; set; }
    public string   DefinitionCode      { get; set; } = default!;
    public string?  ValueLookupItemCode { get; set; }
    public decimal? ValueNumber         { get; set; }
    public string?  ValueText           { get; set; }
    public bool?    ValueBool           { get; set; }
}
