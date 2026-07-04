using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySettlementAutomationRunDetail;

/// <summary>
/// BFF query for a single settlement automation run record with all per-settlement RunItems included.
/// Phase 6 (July 2026).
/// </summary>
public sealed class GetCargoDrySettlementAutomationRunDetailBffQuery
    : AizenQuery<GetCargoDrySettlementAutomationRunDetailBffQueryResponse>
{
    public long RunId { get; init; }
}

public sealed class GetCargoDrySettlementAutomationRunDetailBffQueryResponse
{
    public CargoDrySettlementAutomationRunBffDto Run { get; init; } = default!;
}
