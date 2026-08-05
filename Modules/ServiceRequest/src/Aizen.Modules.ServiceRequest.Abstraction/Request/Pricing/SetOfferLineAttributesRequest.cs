namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Pricing;

/// <summary>S2b — provider payload to set the pricing attribute values on one offer line (full replace for that line).</summary>
public sealed class SetOfferLineAttributesRequest
{
    public List<OfferLineAttributeValueRequest> Attributes { get; set; } = new();
}

public sealed class OfferLineAttributeValueRequest
{
    public string   DefinitionCode      { get; set; } = default!;
    public string?  ValueLookupItemCode { get; set; }
    public decimal? ValueNumber         { get; set; }
    public string?  ValueText           { get; set; }
    public bool?    ValueBool           { get; set; }
}
