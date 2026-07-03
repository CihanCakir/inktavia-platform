using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.CompleteCargoDrySettlementPayout;

/// <summary>
/// BFF command that proxies to the CargoDry commercial module's complete-payout endpoint.
/// Records a successful manual payout and closes the settlement as Settled.
/// THIS IS THE ONLY BFF COMMAND that causes settlement status to advance to Settled.
/// ManualPaymentReference is required. Idempotent if payout is already completed.
/// Requires Phase 4B (PayoutRecord) and Phase 4C (InvoiceId) to be complete first.
/// No gateway call. No automatic transfer. Phase 4D (July 2026).
/// </summary>
public sealed class CompleteCargoDrySettlementPayoutBffCommand
    : AizenCommand<CompleteCargoDrySettlementPayoutBffCommandResponse>
{
    public long    SettlementId             { get; init; }
    public long    CompletedByUserId        { get; init; }
    public string  ManualPaymentReference   { get; init; } = default!;
    public string? Note                     { get; init; }
}

public sealed class CompleteCargoDrySettlementPayoutBffCommandResponse
{
    public CargoDrySellThroughSettlementBffDto? Settlement       { get; init; }
    public CargoDryPayoutLifecycleResultBffDto? PayoutResult     { get; init; }
    /// <summary>True if the settlement was already Settled in a prior call (idempotent return).</summary>
    public bool                                 AlreadyCompleted { get; init; }
}
