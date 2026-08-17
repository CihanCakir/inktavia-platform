using Aizen.Core.Domain;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.Pricing;

/// <summary>
/// S2b (§20.6) — a pricing-attribute <b>value</b> the provider set on one offer line (line-level, consistent with
/// S1/S8). Bound to the offer item by <see cref="OfferItemId"/> and to its schema by <see cref="DefinitionCode"/>. Exactly
/// one of the typed value slots is populated per the definition's DataType (Lookup → <see cref="ValueLookupItemCode"/>,
/// Number → <see cref="ValueNumber"/>, Text → <see cref="ValueText"/>, Boolean → <see cref="ValueBool"/>). Mutable
/// pre-acceptance (the provider edits their offer); at acceptance the value is copied into the immutable
/// <c>OfferLineAttributeSnapshot</c> (S2d). Descriptive — never part of the line money math.
/// </summary>
public sealed class PricingAttributeValueEntity : AizenEntityWithAudit
{
    public long     OfferItemId         { get; private set; }
    public string   DefinitionCode      { get; private set; } = default!;
    public string?  ValueLookupItemCode { get; private set; }
    public decimal? ValueNumber         { get; private set; }
    public string?  ValueText           { get; private set; }
    public bool?    ValueBool           { get; private set; }

    private PricingAttributeValueEntity() { }

    public static PricingAttributeValueEntity Create(
        long offerItemId, string definitionCode,
        string? valueLookupItemCode, decimal? valueNumber, string? valueText, bool? valueBool)
        => new()
        {
            OfferItemId         = offerItemId,
            DefinitionCode      = definitionCode.Trim().ToUpperInvariant(),
            ValueLookupItemCode = string.IsNullOrWhiteSpace(valueLookupItemCode) ? null : valueLookupItemCode.Trim().ToUpperInvariant(),
            ValueNumber         = valueNumber,
            ValueText           = string.IsNullOrWhiteSpace(valueText) ? null : valueText.Trim(),
            ValueBool           = valueBool,
            IsActive            = true,
        };

    public void SetValue(string? valueLookupItemCode, decimal? valueNumber, string? valueText, bool? valueBool)
    {
        ValueLookupItemCode = string.IsNullOrWhiteSpace(valueLookupItemCode) ? null : valueLookupItemCode.Trim().ToUpperInvariant();
        ValueNumber         = valueNumber;
        ValueText           = string.IsNullOrWhiteSpace(valueText) ? null : valueText.Trim();
        ValueBool           = valueBool;
    }
}
