using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryRenewalCandidatesBff;

[DocumentationInfo("Get CargoDry renewal candidates BFF query",
    "Returns Active kits expiring within WithinDays that do not have an open renewal preparation. " +
    "Phase 11 (July 2026).")]
public sealed class GetCargoDryRenewalCandidatesBffQuery
    : AizenQuery<GetCargoDryRenewalCandidatesBffResponse>
{
    public int WithinDays { get; init; } = 90;
    public int Page       { get; init; } = 1;
    public int PageSize   { get; init; } = 50;
}

public sealed class GetCargoDryRenewalCandidatesBffResponse
{
    public List<CargoDryRenewalCandidateBffDto> Candidates { get; init; } = new();
}
