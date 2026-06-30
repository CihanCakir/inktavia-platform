namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

/// <summary>
/// Returned by the Payment module after escrow release.
/// </summary>
public sealed class ReleaseEscrowRemoteCallResponse
{
    public long PayoutRecordId { get; init; }
    public string? GatewayPayoutId { get; init; }
    public decimal ProviderNetAmount { get; init; }
}
