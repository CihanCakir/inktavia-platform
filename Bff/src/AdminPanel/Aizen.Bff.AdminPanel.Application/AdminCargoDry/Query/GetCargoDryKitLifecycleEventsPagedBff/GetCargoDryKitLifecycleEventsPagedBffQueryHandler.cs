using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryKitLifecycleEventsPagedBff;

public sealed class GetCargoDryKitLifecycleEventsPagedBffQueryHandler
    : AizenQueryHandler<GetCargoDryKitLifecycleEventsPagedBffQuery, GetCargoDryKitLifecycleEventsPagedBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDryKitLifecycleEventsPagedBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryKitLifecycleEventsPagedBffResponse> Handle(
        GetCargoDryKitLifecycleEventsPagedBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetKitLifecycleEventsPagedAsync(
            kitId:       request.KitId,
            kitCode:     request.KitCode,
            batchCode:   request.BatchCode,
            productCode: request.ProductCode,
            eventType:   request.EventType,
            actorUserId: request.ActorUserId,
            dateFrom:    request.DateFrom,
            dateTo:      request.DateTo,
            page:        request.Page,
            pageSize:    request.PageSize,
            ct:          ct);

        return new GetCargoDryKitLifecycleEventsPagedBffResponse
        {
            PagedEvents = result ?? new CargoDryKitLifecycleEventsPagedBffResponse
            {
                Page     = request.Page,
                PageSize = request.PageSize,
            },
        };
    }
}
