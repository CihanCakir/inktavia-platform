using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySettlementPayoutExecutionPreview;

/// <summary>
/// BFF query that proxies to the CargoDry commercial module's payout-execution-preview endpoint.
/// Returns all payout lifecycle eligibility flags for Approve, MarkProcessing, Complete, and Fail.
/// Loads live PayoutStatus from the Payment module via the CargoDry module handler.
/// Safe to call at any time — never throws for business ineligibility.
/// Phase 4D (July 2026).
/// </summary>
public sealed class GetCargoDrySettlementPayoutExecutionPreviewBffQuery
    : AizenQuery<GetCargoDrySettlementPayoutExecutionPreviewBffQueryResponse>
{
    public long SettlementId { get; init; }
}

public sealed class GetCargoDrySettlementPayoutExecutionPreviewBffQueryResponse
{
    public CargoDrySettlementPayoutExecutionPreviewBffDto? Preview { get; init; }
}
