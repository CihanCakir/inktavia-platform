using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryKitLifecycleEventsPaged;

/// <summary>
/// Returns a paged, filtered global list of kit lifecycle events.
/// Phase 9A — operational event log.
/// </summary>
public sealed class GetCargoDryKitLifecycleEventsPagedQuery
    : AizenQuery<CargoDryKitLifecycleEventsPagedResponse>
{
    public long?                         KitId       { get; init; }
    public string?                       KitCode     { get; init; }
    public string?                       BatchCode   { get; init; }
    public string?                       ProductCode { get; init; }
    public CargoDryKitLifecycleEventType? EventType  { get; init; }
    public long?                         ActorUserId { get; init; }
    public DateTimeOffset?               DateFrom    { get; init; }
    public DateTimeOffset?               DateTo      { get; init; }
    public int                           Page        { get; init; } = 1;
    public int                           PageSize    { get; init; } = 25;
}
