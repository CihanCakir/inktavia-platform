using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryKitLifecycleHistoryBff;

public sealed class GetCargoDryKitLifecycleHistoryBffQueryHandler
    : AizenQueryHandler<GetCargoDryKitLifecycleHistoryBffQuery, GetCargoDryKitLifecycleHistoryBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDryKitLifecycleHistoryBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryKitLifecycleHistoryBffResponse> Handle(
        GetCargoDryKitLifecycleHistoryBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetKitLifecycleHistoryAsync(request.KitId, ct);

        return new GetCargoDryKitLifecycleHistoryBffResponse
        {
            History = result ?? new CargoDryKitLifecycleHistoryBffResponse
            {
                KitId   = request.KitId,
                KitCode = string.Empty,
            },
        };
    }
}
