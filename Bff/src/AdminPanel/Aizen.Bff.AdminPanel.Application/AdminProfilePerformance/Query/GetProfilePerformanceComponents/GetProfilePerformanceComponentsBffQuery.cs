using Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Query.GetProfilePerformanceComponents;

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
