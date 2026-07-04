using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryKitLifecycleEventsPaged;

public sealed class GetCargoDryKitLifecycleEventsPagedQueryHandler
    : AizenQueryHandler<GetCargoDryKitLifecycleEventsPagedQuery, CargoDryKitLifecycleEventsPagedResponse>
{
    private readonly ICargoDryKitLifecycleEventRepository _lifecycleEvents;

    public GetCargoDryKitLifecycleEventsPagedQueryHandler(
        ICargoDryKitLifecycleEventRepository lifecycleEvents)
        => _lifecycleEvents = lifecycleEvents;

    public override async Task<CargoDryKitLifecycleEventsPagedResponse> Handle(
        GetCargoDryKitLifecycleEventsPagedQuery request, CancellationToken ct)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var page     = Math.Max(request.Page, 1);
        var skip     = (page - 1) * pageSize;

        var (events, total) = await _lifecycleEvents.GetPagedAsync(
            kitId:       request.KitId,
            kitCode:     request.KitCode,
            batchCode:   request.BatchCode,
            productCode: request.ProductCode,
            eventType:   request.EventType,
            actorUserId: request.ActorUserId,
            dateFrom:    request.DateFrom,
            dateTo:      request.DateTo,
            skip:        skip,
            take:        pageSize,
            ct:          ct);

        var items = events.Select(e => new CargoDryKitLifecycleEventDto
        {
            Id             = e.Id,
            KitId          = e.KitId,
            KitCode        = e.KitCode,
            SerialNumber   = e.SerialNumber,
            BatchCode      = e.BatchCode,
            ProductCode    = e.ProductCode,
            EventType      = e.EventType.ToString(),
            PreviousStatus = e.PreviousStatus,
            NewStatus      = e.NewStatus,
            ActorUserId    = e.ActorUserId,
            ActorType      = e.ActorType,
            Reason         = e.Reason,
            Note           = e.Note,
            ReferenceId    = e.ReferenceId,
            ReferenceType  = e.ReferenceType,
            MetadataJson   = e.MetadataJson,
            OccurredAtUtc  = e.OccurredAtUtc,
        }).ToList();

        return new CargoDryKitLifecycleEventsPagedResponse
        {
            Items    = items,
            Total    = total,
            Page     = page,
            PageSize = pageSize,
        };
    }
}
