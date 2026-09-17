namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;

/// <summary>
/// Split-host bridge request: CargoDry asks Payment to prepare (or return the existing) ProviderSettlementStatement
/// draft for a sell-through settlement. Mirrors <c>CreateCargoDrySettlementStatementCommand</c>. Idempotent on the
/// Payment side (a non-cancelled statement for this settlement returns AlreadyExisted=true). Creates a Draft only —
/// does NOT issue the invoice or create a PaymentTransaction.
/// </summary>
public sealed class PrepareCargoDrySettlementStatementRemoteCallRequest
{
    public required long     SettlementId          { get; init; }
    public required string   SettlementCode        { get; init; }
    public required long     ProviderProfileId     { get; init; }
    public required decimal  ProviderPayoutAmount  { get; init; }
    public required decimal  TotalSaleAmount       { get; init; }
    public required decimal  TotalCommissionAmount { get; init; }
    public required int      TotalKitCount         { get; init; }
    public required string   CurrencyCode          { get; init; }
    public required string   ProductCode           { get; init; }
    public required DateTime PeriodStartUtc        { get; init; }
    public required DateTime PeriodEndUtc          { get; init; }
    public          long?    PayoutRecordId        { get; init; }
    public required long     PreparedByUserId      { get; init; }
    public          string?  Notes                 { get; init; }
}
