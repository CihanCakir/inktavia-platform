using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDrySettlementAutomationRunsPaged;

/// <summary>
/// BFF query for paginated settlement automation run history.
/// All filter parameters are optional. Phase 6 (July 2026).
/// </summary>
public sealed class GetCargoDrySettlementAutomationRunsPagedBffQuery
    : AizenQuery<GetCargoDrySettlementAutomationRunsPagedBffQueryResponse>
{
    public int?      TargetYearMonth    { get; init; }
    public int?      Status             { get; init; }
    public int?      Mode               { get; init; }
    public long?     TriggeredByUserId  { get; init; }
    public DateTime? FromUtc            { get; init; }
    public DateTime? ToUtc              { get; init; }
    public int       Skip               { get; init; } = 0;
    public int       Take               { get; init; } = 20;
}

public sealed class GetCargoDrySettlementAutomationRunsPagedBffQueryResponse
{
    public CargoDrySettlementAutomationRunsPagedBffDto Result { get; init; } = default!;
}
