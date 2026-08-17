using Aizen.Core.CQRS.Message;
using Aizen.Modules.Profile.Abstraction.Dtos.Performance;
using Aizen.Modules.Profile.Abstraction.Enums.Performance;

namespace Aizen.Modules.Profile.Application.Queries.Performance.GetRiskSignalsByProfile;

public sealed class GetRiskSignalsByProfileQuery
    : AizenQuery<ProfileRiskSignalPagedResultDto>
{
    public long        ProfileId   { get; init; }
    public ProfileType ProfileType { get; init; }
    /// <summary>When true, returns only unresolved signals. When false/null, returns all.</summary>
    public bool?       ActiveOnly  { get; init; }
    public int         Page        { get; init; } = 1;
    public int         PageSize    { get; init; } = 25;
}
