using Aizen.Core.CQRS.Message;
using Aizen.Modules.Profile.Abstraction.Dtos.Performance;
using Aizen.Modules.Profile.Abstraction.Enums.Performance;

namespace Aizen.Modules.Profile.Application.Queries.Performance.GetDecisionLogsByProfile;

public sealed class GetDecisionLogsByProfileQuery
    : AizenQuery<ProfileDecisionLogPagedResultDto>
{
    public long        ProfileId   { get; init; }
    public ProfileType ProfileType { get; init; }
    public int         Page        { get; init; } = 1;
    public int         PageSize    { get; init; } = 25;
}
