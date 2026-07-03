using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.PrepareCargoDrySettlementInvoice;

/// <summary>
/// BFF command that proxies to the CargoDry commercial module's prepare-invoice endpoint.
/// Creates a Draft ProviderSettlementStatement invoice in the Payment module.
/// Settlement status remains Scheduled after preparation.
/// Idempotent — safe to call multiple times; returns existing invoice if already prepared.
/// Phase 4C (July 2026).
/// </summary>
public sealed class PrepareCargoDrySettlementInvoiceBffCommand
    : AizenCommand<PrepareCargoDrySettlementInvoiceBffCommandResponse>
{
    public long    SettlementId     { get; init; }
    public long    PreparedByUserId { get; init; }
    public string? PreparationNote  { get; init; }
}

public sealed class PrepareCargoDrySettlementInvoiceBffCommandResponse
{
    public CargoDrySellThroughSettlementBffDto? Settlement     { get; init; }
    public long                                 InvoiceId      { get; init; }
    public bool                                 AlreadyExisted { get; init; }
}
