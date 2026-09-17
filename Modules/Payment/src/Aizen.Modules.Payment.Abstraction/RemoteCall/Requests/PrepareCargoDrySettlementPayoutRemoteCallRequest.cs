namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;

/// <summary>
/// Split-host bridge request: CargoDry asks Payment to prepare (or return the existing) PayoutRecord for a monthly
/// sell-through settlement. Mirrors <c>CreateCargoDrySettlementPayoutPreparationCommand</c>. Idempotent by
/// <see cref="SourceSettlementId"/> on the Payment side (partial-unique index) — a re-prepare returns the existing record.
/// </summary>
public sealed class PrepareCargoDrySettlementPayoutRemoteCallRequest
{
    public required long    SourceSettlementId { get; init; }
    public required string  SettlementCode     { get; init; }
    public required long    ProviderProfileId  { get; init; }
    public required decimal Amount             { get; init; }
    public required string  CurrencyCode       { get; init; }
    public required string  Description        { get; init; }
    public required long    PreparedByUserId   { get; init; }
}
