using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySettlementAutomationRunsPaged;

/// <summary>
/// Paged list of CargoDry settlement automation run records.
/// Phase 6 (July 2026).
/// </summary>
[DocumentationInfo("Get CargoDry settlement automation runs paged query",
    "Returns a paged list of automation run records with optional filters. " +
    "Run items are NOT included in the paged list — use GetCargoDrySettlementAutomationRunDetail for items. " +
    "Phase 6 (July 2026).")]
public sealed class GetCargoDrySettlementAutomationRunsPagedQuery
    : AizenQuery<GetCargoDrySettlementAutomationRunsPagedQueryResponse>
{
    public int?                                   TargetYearMonth   { get; init; }
    public CargoDrySettlementAutomationRunStatus? Status            { get; init; }
    public CargoDrySettlementAutomationMode?      Mode              { get; init; }
    public long?                                  TriggeredByUserId { get; init; }
    public DateTime?                              FromUtc           { get; init; }
    public DateTime?                              ToUtc             { get; init; }
    public int                                    Page              { get; init; } = 1;
    public int                                    PageSize          { get; init; } = 25;
}

public sealed class GetCargoDrySettlementAutomationRunsPagedQueryResponse
{
    public List<CargoDrySettlementAutomationRunDto> Items    { get; init; } = [];
    public int                                      Total    { get; init; }
    public int                                      Page     { get; init; }
    public int                                      PageSize { get; init; }
}
