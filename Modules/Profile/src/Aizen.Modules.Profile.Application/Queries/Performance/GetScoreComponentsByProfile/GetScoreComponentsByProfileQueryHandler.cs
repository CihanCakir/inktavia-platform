using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Profile.Abstraction.Dtos.Performance;
using Aizen.Modules.Profile.Domain.Interface.Repository;

namespace Aizen.Modules.Profile.Application.Queries.Performance.GetScoreComponentsByProfile;

[DocumentationInfo("GetScoreComponentsByProfileQueryHandler",
    "Returns per-dimension score components (5 rows) for the current snapshot of a given profile. " +
    "Components store the raw score, weight, weighted contribution, and individual metric JSON for each dimension. " +
    "Returns empty list if no snapshot exists.")]
public sealed class GetScoreComponentsByProfileQueryHandler
    : AizenQueryHandler<GetScoreComponentsByProfileQuery, GetScoreComponentsByProfileResponse>
{
    private readonly IProfileScoreComponentRepository _components;

    public GetScoreComponentsByProfileQueryHandler(IProfileScoreComponentRepository components)
    {
        _components = components;
    }

    public override async Task<GetScoreComponentsByProfileResponse> Handle(
        GetScoreComponentsByProfileQuery request, CancellationToken ct)
    {
        var items = await _components.GetByProfileAsync(request.ProfileId, request.ProfileType, ct);

        return new GetScoreComponentsByProfileResponse
        {
            Components = items.Select(c => new ProfileScoreComponentDto
            {
                SnapshotId           = c.SnapshotId,
                Category             = c.Category,
                RawScore             = c.RawScore,
                Weight               = c.Weight,
                WeightedContribution = c.WeightedContribution,
                MetricCount          = c.MetricCount,
                MetricsJson          = c.MetricsJson,
                CalculatedAtUtc      = c.CalculatedAtUtc,
            }).ToList(),
        };
    }
}
