using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryKitLifecycleHistory;

public sealed class GetCargoDryKitLifecycleHistoryQueryHandler
    : AizenQueryHandler<GetCargoDryKitLifecycleHistoryQuery, CargoDryKitLifecycleHistoryResponse>
{
    private readonly ICargoDryKitLifecycleEventRepository _lifecycleEvents;

    public GetCargoDryKitLifecycleHistoryQueryHandler(
        ICargoDryKitLifecycleEventRepository lifecycleEvents)
        => _lifecycleEvents = lifecycleEvents;

    public override async Task<CargoDryKitLifecycleHistoryResponse> Handle(
        GetCargoDryKitLifecycleHistoryQuery request, CancellationToken ct)
    {
        var events = await _lifecycleEvents.GetByKitIdAsync(request.KitId, ct);

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

        return new CargoDryKitLifecycleHistoryResponse
        {
            KitId  = request.KitId,
            Events = items,
        };
    }
}
