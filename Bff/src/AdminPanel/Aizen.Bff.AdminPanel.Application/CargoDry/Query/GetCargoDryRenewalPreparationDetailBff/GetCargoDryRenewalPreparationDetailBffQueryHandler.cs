using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryRenewalPreparationDetailBff;

[DocumentationInfo("Get CargoDry renewal preparation detail BFF query handler",
    "Calls the CargoDry admin renewals/{id} endpoint and returns the full preparation record. " +
    "Phase 11 (July 2026).")]
public sealed class GetCargoDryRenewalPreparationDetailBffQueryHandler
    : AizenQueryHandler<GetCargoDryRenewalPreparationDetailBffQuery, GetCargoDryRenewalPreparationDetailBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDryRenewalPreparationDetailBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryRenewalPreparationDetailBffResponse> Handle(
        GetCargoDryRenewalPreparationDetailBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetRenewalPreparationDetailAsync(request.Id, ct);
        return new GetCargoDryRenewalPreparationDetailBffResponse { Preparation = result };
    }
}
