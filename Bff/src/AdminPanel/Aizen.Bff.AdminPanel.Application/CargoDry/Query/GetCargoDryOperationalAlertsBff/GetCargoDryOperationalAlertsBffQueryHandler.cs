using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryOperationalAlertsBff;

public sealed class GetCargoDryOperationalAlertsBffQueryHandler
    : AizenQueryHandler<GetCargoDryOperationalAlertsBffQuery, GetCargoDryOperationalAlertsBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDryOperationalAlertsBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryOperationalAlertsBffResponse> Handle(
        GetCargoDryOperationalAlertsBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetOperationalAlertsAsync(
            page:     request.Page,
            pageSize: request.PageSize,
            ct:       ct);

        return new GetCargoDryOperationalAlertsBffResponse
        {
            Alerts = result ?? new CargoDryOperationalAlertsBffResponse
            {
                Page     = request.Page,
                PageSize = request.PageSize,
            },
        };
    }
}
