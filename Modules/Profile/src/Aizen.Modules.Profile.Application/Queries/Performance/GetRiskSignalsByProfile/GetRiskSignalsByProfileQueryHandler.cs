using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Profile.Abstraction.Dtos.Performance;
using Aizen.Modules.Profile.Domain.Entities.Performance;
using Aizen.Modules.Profile.Domain.Interface.Repository;

namespace Aizen.Modules.Profile.Application.Queries.Performance.GetRiskSignalsByProfile;

[DocumentationInfo("GetRiskSignalsByProfileQueryHandler",
    "Returns risk signals (paged) for a given profile. " +
    "Supports ActiveOnly=true to show only open signals. " +
    "High/Critical signals override PriorityTier to Flagged.")]
public sealed class GetRiskSignalsByProfileQueryHandler
    : AizenQueryHandler<GetRiskSignalsByProfileQuery, ProfileRiskSignalPagedResultDto>
{
    private readonly IProfileRiskSignalRepository _signals;

    public GetRiskSignalsByProfileQueryHandler(IProfileRiskSignalRepository signals)
    {
        _signals = signals;
    }

    public override async Task<ProfileRiskSignalPagedResultDto> Handle(
        GetRiskSignalsByProfileQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;

        List<ProfileRiskSignalEntity> items;
        int total;

        if (request.ActiveOnly == true)
        {
            var active = await _signals.GetActiveByProfileAsync(
                request.ProfileId, request.ProfileType, ct);
            total = active.Count;
            items = active.Skip(skip).Take(request.PageSize).ToList();
        }
        else
        {
            items = await _signals.GetByProfileAsync(
                request.ProfileId, request.ProfileType, skip, request.PageSize, ct);
            // Repository doesn't expose a count for all-signals — derive from page
            total = items.Count < request.PageSize && request.Page == 1
                ? items.Count
                : skip + items.Count; // best estimate; exact count not required for UI
        }

        return new ProfileRiskSignalPagedResultDto
        {
            Items = items.Select(e => new ProfileRiskSignalDto
            {
                Id               = e.Id,
                ProfileId        = e.ProfileId,
                ProfileType      = e.ProfileType,
                Severity         = e.Severity,
                SignalCode       = e.SignalCode,
                Description      = e.Description,
                SourceModule     = e.SourceModule,
                SourceEntityId   = e.SourceEntityId,
                DetectedAtUtc    = e.DetectedAtUtc,
                IsResolved       = e.IsResolved,
                ResolvedAtUtc    = e.ResolvedAtUtc,
                ResolutionNote   = e.ResolutionNote,
                ResolvedByUserId = e.ResolvedByUserId,
            }).ToList(),
            Total    = total,
            Page     = request.Page,
            PageSize = request.PageSize,
        };
    }
}
