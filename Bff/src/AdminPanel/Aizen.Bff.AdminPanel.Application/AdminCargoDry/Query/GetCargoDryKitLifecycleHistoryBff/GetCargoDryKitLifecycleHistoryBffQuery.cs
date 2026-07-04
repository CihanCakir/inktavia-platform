using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryKitLifecycleHistoryBff;

public sealed class GetCargoDryKitLifecycleHistoryBffQuery
    : AizenQuery<GetCargoDryKitLifecycleHistoryBffResponse>
{
    public long KitId { get; init; }
}

public sealed class GetCargoDryKitLifecycleHistoryBffResponse
{
    public CargoDryKitLifecycleHistoryBffResponse History { get; init; } = default!;
}
