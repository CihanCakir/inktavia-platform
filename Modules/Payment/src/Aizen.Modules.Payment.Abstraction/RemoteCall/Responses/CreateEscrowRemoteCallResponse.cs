namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

/// <summary>
/// Returned by the Payment module after escrow creation.
/// ServiceRequest module must persist TransactionId on the SR entity.
/// </summary>
public sealed class CreateEscrowRemoteCallResponse
{
    public long TransactionId { get; init; }
    public string TransactionCode { get; init; } = string.Empty;
    public string GatewayReference { get; init; } = string.Empty;
    public decimal CommissionRate { get; init; }
    public decimal CommissionAmount { get; init; }
    public decimal NetPayoutAmount { get; init; }
}
