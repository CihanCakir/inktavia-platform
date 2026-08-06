using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminFinance.Query.GetCargoDrySettlementReconciliationBff;

[DocumentationInfo("Get CargoDry settlement reconciliation report BFF query handler",
    "Proxies the admin settlement reconciliation request to the CargoDry module finance endpoint. " +
    "Returns paged settlement rows with server-side mismatch flags for admin finance auditing. " +
    "Phase 15 (July 2026).")]
public sealed class GetCargoDrySettlementReconciliationBffQueryHandler
    : AizenQueryHandler<GetCargoDrySettlementReconciliationBffQuery, GetCargoDrySettlementReconciliationBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDrySettlementReconciliationBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDrySettlementReconciliationBffResponse> Handle(
        GetCargoDrySettlementReconciliationBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetSettlementReconciliationAsync(
            request.ProviderProfileId,
            request.ProductCode,
            request.Status,
            request.HasMismatches,
            request.DateFrom,
            request.DateTo,
            request.Page,
            request.PageSize,
            ct);

        return new GetCargoDrySettlementReconciliationBffResponse { Report = result };
    }
}
