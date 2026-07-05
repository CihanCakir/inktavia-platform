using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryRenewalPreparationDetailBff;

[DocumentationInfo("Get CargoDry renewal preparation detail BFF query",
    "Returns the full detail of a single renewal preparation by Id. " +
    "Phase 11 (July 2026).")]
public sealed class GetCargoDryRenewalPreparationDetailBffQuery
    : AizenQuery<GetCargoDryRenewalPreparationDetailBffResponse>
{
    public required long Id { get; init; }
}

public sealed class GetCargoDryRenewalPreparationDetailBffResponse
{
    public CargoDryRenewalPreparationBffDto? Preparation { get; init; }
}
