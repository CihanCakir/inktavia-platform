namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

/// <summary>
/// BE-S3 — one offer-level FX rate snapshot row (per non-TRY source currency), surfaced for the provider/admin FX display.
/// The rate is <c>1 <see cref="SourceCurrencyCode"/> = <see cref="Rate"/> <see cref="SettlementCurrencyCode"/></c>, resolved
/// at submit and frozen at acceptance. Read-only projection of <c>OfferFxSnapshotEntity</c>.
/// </summary>
[DocumentationInfo("Offer FX snapshot DTO", "Per-source-currency exchange-rate snapshot on an offer (source ccy, settlement ccy, rate, rate date).")]
public sealed class OfferFxSnapshotDto
{
    public string   SourceCurrencyCode     { get; set; } = default!;
    public string   SettlementCurrencyCode { get; set; } = default!;
    public decimal  Rate                   { get; set; }
    public DateTime RateDate               { get; set; }
}
