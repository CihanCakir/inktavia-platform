using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySettlementAutomationRunDetail;

/// <summary>
/// Returns full detail of a single CargoDry settlement automation run including all run items.
/// Phase 6 (July 2026).
/// </summary>
[DocumentationInfo("Get CargoDry settlement automation run detail query",
    "Returns a single automation run record with all per-settlement run items included. " +
    "Phase 6 (July 2026).")]
public sealed class GetCargoDrySettlementAutomationRunDetailQuery
    : AizenQuery<GetCargoDrySettlementAutomationRunDetailQueryResponse>
{
    public long RunId { get; init; }
}

public sealed class GetCargoDrySettlementAutomationRunDetailQueryResponse
{
    public CargoDrySettlementAutomationRunDto Run { get; init; } = default!;
}
