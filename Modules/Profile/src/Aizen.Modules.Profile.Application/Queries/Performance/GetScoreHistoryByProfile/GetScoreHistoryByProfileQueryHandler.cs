using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Profile.Abstraction.Dtos.Performance;
using Aizen.Modules.Profile.Domain.Interface.Repository;

namespace Aizen.Modules.Profile.Application.Queries.Performance.GetScoreHistoryByProfile;

[DocumentationInfo("GetScoreHistoryByProfileQueryHandler",
    "Returns append-only score history (paged) for a given profile. " +
    "Used for trend charts and tier-change audit in the admin panel.")]
public sealed class GetScoreHistoryByProfileQueryHandler
    : AizenQueryHandler<GetScoreHistoryByProfileQuery, ProfileScoreHistoryPagedResultDto>
{
    private readonly IProfileScoreHistoryRepository _history;

    public GetScoreHistoryByProfileQueryHandler(IProfileScoreHistoryRepository history)
    {
        _history = history;
    }

    public override async Task<ProfileScoreHistoryPagedResultDto> Handle(
        GetScoreHistoryByProfileQuery request, CancellationToken ct)
    {
        var skip  = (request.Page - 1) * request.PageSize;
        var total = await _history.CountByProfileAsync(request.ProfileId, request.ProfileType, ct);
        var items = await _history.GetByProfileAsync(
            request.ProfileId, request.ProfileType, skip, request.PageSize, ct);

        return new ProfileScoreHistoryPagedResultDto
        {
            Items = items.Select(e => new ProfileScoreHistoryDto
            {
                Id              = e.Id,
                ProfileId       = e.ProfileId,
                ProfileType     = e.ProfileType,
                OverallScore    = e.OverallScore,
                PriorityTier    = e.PriorityTier,
                ConfidenceScore = e.ConfidenceScore,
                SampleSize      = e.SampleSize,
                RecordedAtUtc   = e.RecordedAtUtc,
                TriggerReason   = e.TriggerReason,
            }).ToList(),
            Total    = total,
            Page     = request.Page,
            PageSize = request.PageSize,
        };
    }
}
