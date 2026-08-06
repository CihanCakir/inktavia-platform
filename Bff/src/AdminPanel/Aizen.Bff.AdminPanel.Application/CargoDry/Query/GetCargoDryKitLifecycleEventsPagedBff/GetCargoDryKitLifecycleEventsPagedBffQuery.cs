using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryKitLifecycleEventsPagedBff;

public sealed class GetCargoDryKitLifecycleEventsPagedBffQuery
    : AizenQuery<GetCargoDryKitLifecycleEventsPagedBffResponse>
{
    public long?           KitId       { get; init; }
    public string?         KitCode     { get; init; }
    public string?         BatchCode   { get; init; }
    public string?         ProductCode { get; init; }
    public string?         EventType   { get; init; }
    public long?           ActorUserId { get; init; }
    public DateTimeOffset? DateFrom    { get; init; }
    public DateTimeOffset? DateTo      { get; init; }
    public int             Page        { get; init; } = 1;
    public int             PageSize    { get; init; } = 25;
}

public sealed class GetCargoDryKitLifecycleEventsPagedBffResponse
{
    public CargoDryKitLifecycleEventsPagedBffResponse PagedEvents { get; init; } = default!;
}
