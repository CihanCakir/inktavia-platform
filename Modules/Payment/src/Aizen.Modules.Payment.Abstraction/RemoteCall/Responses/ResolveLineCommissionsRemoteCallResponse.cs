namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

/// <summary>
/// Response: per-line commission resolution + transaction aggregates (BE-S7). Commission is rounded PER LINE then
/// summed (§13.6 / §20.15) — the transaction totals are the sum of the rounded line amounts (no blended re-round), so
/// P8/S8 reconcile to the same number. Typed DTOs (never <c>object</c>).
/// </summary>
public sealed class ResolveLineCommissionsRemoteCallResponse
{
    public required List<ResolveLineCommissionResultDto> Lines { get; init; } = new();

    public decimal TransactionCommission     { get; init; }
    public decimal TransactionProviderNet    { get; init; }
    public decimal TransactionCommissionBase { get; init; }
    public string  CurrencyCode              { get; init; } = "TRY";
}

/// <summary>Resolved commission for a single line (BE-S7 §1).</summary>
public sealed class ResolveLineCommissionResultDto
{
    public required string LineRef              { get; init; }
    public bool            Commissionable       { get; init; }
    public decimal         CommissionBaseAmount { get; init; }
    public decimal         ResolvedRate         { get; init; }
    public string?         RuleCode             { get; init; }
    public decimal         CommissionAmount     { get; init; }
    public decimal         ProviderNet          { get; init; }
}
