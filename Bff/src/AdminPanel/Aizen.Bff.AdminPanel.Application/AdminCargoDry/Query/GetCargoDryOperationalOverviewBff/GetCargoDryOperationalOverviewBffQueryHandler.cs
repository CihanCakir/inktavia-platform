using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryOperationalOverviewBff;

public sealed class GetCargoDryOperationalOverviewBffQueryHandler
    : AizenQueryHandler<GetCargoDryOperationalOverviewBffQuery, GetCargoDryOperationalOverviewBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDryOperationalOverviewBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryOperationalOverviewBffResponse> Handle(
        GetCargoDryOperationalOverviewBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetOperationalOverviewAsync(ct);

        return new GetCargoDryOperationalOverviewBffResponse
        {
            Overview = result ?? new CargoDryOperationalOverviewBffDto
            {
                ComputedAtUtc = DateTimeOffset.UtcNow,
            },
        };
    }
}
