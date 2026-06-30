using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.Payment.Application.Commands.ReleasePaymentEscrow;

/// <summary>
/// Called after ServiceRequest completion is approved.
/// Transitions Captured → Released and creates a PayoutRecord for the provider.
/// Commission was frozen at escrow creation — never recalculated here.
/// </summary>
public sealed class ReleasePaymentEscrowCommand : AizenCommand<ReleasePaymentEscrowResult>
{
    public required long   TransactionId    { get; init; }
    public required long   ApprovedByUserId { get; init; }
    public string?         AdminNote        { get; init; }
}

public sealed record ReleasePaymentEscrowResult(
    long   PayoutRecordId,
    string GatewayPayoutId,
    decimal ProviderNetAmount
);
