using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.PrepareCargoDrySettlementPayment;

/// <summary>
/// BFF command that proxies to the CargoDry commercial module's prepare-payment endpoint.
/// Creates a PayoutRecord in the Payment module and transitions the settlement to Scheduled status.
/// Idempotent — safe to call multiple times; returns existing payout record if already prepared.
/// Phase 4B (July 2026).
/// </summary>
public sealed class PrepareCargoDrySettlementPaymentBffCommand
    : AizenCommand<PrepareCargoDrySettlementPaymentBffCommandResponse>
{
    public long    SettlementId     { get; init; }
    public long    PreparedByUserId { get; init; }
    public string? PreparationNote  { get; init; }
}

public sealed class PrepareCargoDrySettlementPaymentBffCommandResponse
{
    public CargoDrySellThroughSettlementBffDto? Settlement     { get; init; }
    public long                                 PayoutRecordId { get; init; }
    public bool                                 AlreadyExisted { get; init; }
}
