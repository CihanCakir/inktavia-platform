using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminFinance.Query.GetCargoDryRenewalReconciliationBff;

[DocumentationInfo("Get CargoDry renewal reconciliation report BFF query handler",
    "Proxies the admin renewal reconciliation request to the CargoDry module finance endpoint. " +
    "Returns paged renewal rows with server-side mismatch flags for admin finance auditing. " +
    "Phase 15 (July 2026).")]
public sealed class GetCargoDryRenewalReconciliationBffQueryHandler
    : AizenQueryHandler<GetCargoDryRenewalReconciliationBffQuery, GetCargoDryRenewalReconciliationBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDryRenewalReconciliationBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryRenewalReconciliationBffResponse> Handle(
        GetCargoDryRenewalReconciliationBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetRenewalReconciliationAsync(
            request.ProductCode,
            request.OwnerUserId,
            request.VesselId,
            request.Status,
            request.NotificationStatus,
            request.HasMismatches,
            request.DateFrom,
            request.DateTo,
            request.Page,
            request.PageSize,
            ct);

        return new GetCargoDryRenewalReconciliationBffResponse { Report = result };
    }
}
