using Aizen.Core.CQRS.Message;
using Aizen.Modules.Profile.Abstraction.Dtos.Performance;
using Aizen.Modules.Profile.Abstraction.Enums.Performance;

namespace Aizen.Modules.Profile.Application.Queries.Performance.GetScoreComponentsByProfile;

public sealed class GetScoreComponentsByProfileQuery
    : AizenQuery<GetScoreComponentsByProfileResponse>
{
    public long        ProfileId   { get; init; }
    public ProfileType ProfileType { get; init; }
}

public sealed record GetScoreComponentsByProfileResponse
{
    public IReadOnlyList<ProfileScoreComponentDto> Components { get; init; } = [];
}
