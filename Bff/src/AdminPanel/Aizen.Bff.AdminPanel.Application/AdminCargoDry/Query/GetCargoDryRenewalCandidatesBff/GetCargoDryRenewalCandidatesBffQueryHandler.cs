using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryRenewalCandidatesBff;

[DocumentationInfo("Get CargoDry renewal candidates BFF query handler",
    "Calls the CargoDry admin renewals/candidates endpoint and returns the candidate list. " +
    "Phase 11 (July 2026).")]
public sealed class GetCargoDryRenewalCandidatesBffQueryHandler
    : AizenQueryHandler<GetCargoDryRenewalCandidatesBffQuery, GetCargoDryRenewalCandidatesBffResponse>
{
    private readonly IAdminCargoDryBffRemoteCall _remote;

    public GetCargoDryRenewalCandidatesBffQueryHandler(IAdminCargoDryBffRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryRenewalCandidatesBffResponse> Handle(
        GetCargoDryRenewalCandidatesBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetRenewalCandidatesAsync(
            request.WithinDays,
            request.Page,
            request.PageSize,
            ct);

        return new GetCargoDryRenewalCandidatesBffResponse
        {
            Candidates = result ?? new(),
        };
    }
}
