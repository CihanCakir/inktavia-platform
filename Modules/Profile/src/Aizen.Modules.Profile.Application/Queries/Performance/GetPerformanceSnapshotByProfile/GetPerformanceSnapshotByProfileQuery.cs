using Aizen.Core.CQRS.Message;
using Aizen.Modules.Profile.Abstraction.Dtos.Performance;
using Aizen.Modules.Profile.Abstraction.Enums.Performance;

namespace Aizen.Modules.Profile.Application.Queries.Performance.GetPerformanceSnapshotByProfile;

public sealed class GetPerformanceSnapshotByProfileQuery
    : AizenQuery<GetPerformanceSnapshotByProfileResponse>
{
    public long        ProfileId   { get; init; }
    public ProfileType ProfileType { get; init; }
}

public sealed record GetPerformanceSnapshotByProfileResponse
{
    public ProfilePerformanceSnapshotDto?          Snapshot   { get; init; }
    public IReadOnlyList<ProfileScoreComponentDto> Components { get; init; } = [];
    public bool                                    Found      { get; init; }
}
