using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Profile.Abstraction.Dtos.Performance;
using Aizen.Modules.Profile.Domain.Interface.Repository;

namespace Aizen.Modules.Profile.Application.Queries.Performance.GetDecisionLogsByProfile;

[DocumentationInfo("GetDecisionLogsByProfileQueryHandler",
    "Returns append-only performance decision log (paged) for a given profile. " +
    "Covers score recalculations, tier changes, risk signal events, and manual overrides.")]
public sealed class GetDecisionLogsByProfileQueryHandler
    : AizenQueryHandler<GetDecisionLogsByProfileQuery, ProfileDecisionLogPagedResultDto>
{
    private readonly IProfileDecisionLogRepository _logs;

    public GetDecisionLogsByProfileQueryHandler(IProfileDecisionLogRepository logs)
    {
        _logs = logs;
    }

    public override async Task<ProfileDecisionLogPagedResultDto> Handle(
        GetDecisionLogsByProfileQuery request, CancellationToken ct)
    {
        var skip  = (request.Page - 1) * request.PageSize;
        var total = await _logs.CountByProfileAsync(request.ProfileId, request.ProfileType, ct);
        var items = await _logs.GetByProfileAsync(
            request.ProfileId, request.ProfileType, skip, request.PageSize, ct);

        return new ProfileDecisionLogPagedResultDto
        {
            Items = items.Select(e => new ProfileDecisionLogDto
            {
                Id               = e.Id,
                ProfileId        = e.ProfileId,
                ProfileType      = e.ProfileType,
                EventType        = e.EventType,
                EventDescription = e.EventDescription,
                PreviousTier     = e.PreviousTier,
                NewTier          = e.NewTier,
                PreviousScore    = e.PreviousScore,
                NewScore         = e.NewScore,
                ActorUserId      = e.ActorUserId,
                OccurredAtUtc    = e.OccurredAtUtc,
                MetadataJson     = e.MetadataJson,
            }).ToList(),
            Total    = total,
            Page     = request.Page,
            PageSize = request.PageSize,
        };
    }
}
