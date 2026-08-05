using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("ServiceRequest offer item DTO", "Represents a single line item within a provider offer.")]
public sealed class ServiceRequestOfferItemDto
{
    public long Id { get; set; }
    public ServiceRequestOfferItemType ItemType { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    /// <summary>The unit price the economics uses — in the settlement currency (TRY) once a foreign line has been converted at submit.</summary>
    public decimal UnitPrice { get; set; }
    /// <summary>The line's source currency the provider quoted in (e.g. "EUR"). For a converted line this describes <see cref="SourceUnitPrice"/>, not <see cref="UnitPrice"/>.</summary>
    public string CurrencyCode { get; set; } = "USD";

    // ── BE-S3 — FX transparency: both the source figure and the converted TRY figure ──
    /// <summary>Raw foreign unit price before conversion; null ⇒ the line is settlement-native (no conversion happened).</summary>
    public decimal? SourceUnitPrice { get; set; }
    /// <summary>The source currency of <see cref="SourceUnitPrice"/> (= <see cref="CurrencyCode"/>); null when the line was not converted.</summary>
    public string? SourceCurrencyCode { get; set; }
    /// <summary>The currency <see cref="UnitPrice"/> is denominated in — always the platform settlement currency (TRY).</summary>
    public string SettlementCurrencyCode { get; set; } = "TRY";

    public int SortOrder { get; set; }
    public string? UnitCode { get; set; }
    public decimal TaxRate { get; set; }
    public OfferDiscountType DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal LineSubtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal DiscountAmount { get; set; }
}
