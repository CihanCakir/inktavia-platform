using Aizen.Bff.AdminPanel.Application.ProfilePerformance.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ProfilePerformance.Query.GetProfilePerformanceComponents;

public sealed class GetProfilePerformanceComponentsBffQuery
    : AizenQuery<GetProfilePerformanceComponentsBffResponse>
{
    public long   ProfileId   { get; init; }
    public string ProfileType { get; init; } = "Provider";
}

public sealed class GetProfilePerformanceComponentsBffResponse
{
    public List<ProfileScoreComponentBffDto> Data { get; init; } = [];
}
