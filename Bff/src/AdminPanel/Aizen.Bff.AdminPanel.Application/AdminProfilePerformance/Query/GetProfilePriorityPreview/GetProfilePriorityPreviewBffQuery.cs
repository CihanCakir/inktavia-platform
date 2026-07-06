using Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Query.GetProfilePriorityPreview;

/// <summary>
/// Phase 21 — BFF proxy query for priority preview.
/// Forwards the request to the Profile module endpoint and returns the result as-is.
/// BFF rule: proxy-only — no score calculation here.
/// </summary>
public sealed class GetProfilePriorityPreviewBffQuery : AizenQuery<GetProfilePriorityPreviewBffResponse>
{
    public List<long> CandidateProfileIds { get; init; } = [];
    public string     Context             { get; init; } = default!;
    public string?    CategoryCode        { get; init; }
    public string?    LocationCode        { get; init; }
    public int        MaxResults          { get; init; } = 10;
    public bool       LogDecision         { get; init; } = false;
}

public sealed class GetProfilePriorityPreviewBffResponse
{
    public ProfilePriorityPreviewBffResult Data { get; init; } = default!;
}
