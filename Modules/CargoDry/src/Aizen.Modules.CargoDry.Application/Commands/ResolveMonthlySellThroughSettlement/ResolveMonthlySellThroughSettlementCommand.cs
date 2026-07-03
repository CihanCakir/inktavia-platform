using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.ResolveMonthlySellThroughSettlement;

/// <summary>
/// Finalizes a monthly sell-through settlement by:
///   1. Verifying all linked attributions are financially resolved.
///   2. Recalculating settlement totals from resolved attribution amounts.
///   3. Marking the settlement as ReadyForSettlement (pending payout scheduling).
///
/// Blocks if any attribution in the settlement still has null financial amounts.
/// Does NOT create a PaymentTransaction, Invoice, or ProviderPayout record.
///
/// Phase 4A (July 2026): CargoDry Financial Resolution.
/// </summary>
[DocumentationInfo("Resolve monthly sell-through settlement command",
    "Verifies all attribution financials are resolved, recalculates settlement totals, " +
    "and transitions the settlement to ReadyForSettlement status. " +
    "Phase 4A (July 2026).")]
public sealed class ResolveMonthlySellThroughSettlementCommand
    : AizenCommand<ResolveMonthlySellThroughSettlementResponse>
{
    /// <summary>Id of the CargoDrySellThroughSettlementEntity to finalize.</summary>
    public long    SettlementId      { get; init; }

    /// <summary>Admin user performing the resolution. Required for audit trail.</summary>
    public long    ResolvedByUserId  { get; init; }

    /// <summary>Optional admin note to attach to the settlement.</summary>
    public string? ResolutionNote    { get; init; }
}

public sealed class ResolveMonthlySellThroughSettlementResponse
{
    public CargoDrySellThroughSettlementDto Settlement { get; init; } = default!;
}
