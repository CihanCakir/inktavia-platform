using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryKitLifecycleHistory;

/// <summary>
/// Returns all lifecycle events for a single kit, newest-first.
/// Phase 9A — kit history timeline.
/// </summary>
public sealed class GetCargoDryKitLifecycleHistoryQuery : AizenQuery<CargoDryKitLifecycleHistoryResponse>
{
    public long KitId { get; init; }
}
