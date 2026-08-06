using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

[DocumentationInfo("Get admin service request operation detail query handler",
    "Fetches the full service request detail for the admin operation panel, enriched with vessel name and provider display names.")]
public sealed class GetServiceRequestOperationDetailBffQueryHandler
    : AizenQueryHandler<GetServiceRequestOperationDetailBffQuery, AdminServiceRequestOperationDetailResponse>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;
    private readonly IVesselRemoteCall         _vessel;
    private readonly IIdentityRemoteCall       _identity;

    public GetServiceRequestOperationDetailBffQueryHandler(
        IServiceRequestRemoteCall serviceRequest,
        IVesselRemoteCall         vessel,
        IIdentityRemoteCall       identity)
    {
        _serviceRequest = serviceRequest;
        _vessel         = vessel;
        _identity       = identity;
    }

    public override async Task<AdminServiceRequestOperationDetailResponse?> Handle(
        GetServiceRequestOperationDetailBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminServiceRequestOperationDetailResponse();

        try
        {
            var result = await _serviceRequest.GetAdminServiceRequestDetail(request.ServiceRequestId);
            response.ServiceRequest = result.Body;
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ServiceRequest"));
            return response;
        }

        var detail = response.ServiceRequest?.Detail;
        if (detail is null)
            return response;

        // ── Collect IDs for parallel enrichment ──────────────────────────────
        var vesselId = detail.Request?.VesselId ?? 0;

        var providerUserIds = (detail.Offers ?? [])
            .Select(o => o.ProviderUserId)
            .Concat(detail.Assignment is not null ? [detail.Assignment.ProviderUserId] : Array.Empty<long>())
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        // ── Parallel calls — failures are swallowed, frontend falls back to IDs ──
        var vesselTask   = vesselId > 0              ? FetchVesselNameAsync(vesselId, cancellationToken)          : Task.FromResult<string?>(null);
        var identityTask = providerUserIds.Length > 0 ? FetchProviderNamesAsync(providerUserIds, cancellationToken) : Task.FromResult(new Dictionary<long, string>());

        await Task.WhenAll(vesselTask, identityTask);

        response.VesselName    = await vesselTask;
        response.ProviderNames = await identityTask;

        return response;
    }

    private async Task<string?> FetchVesselNameAsync(long vesselId, CancellationToken ct)
    {
        try
        {
            var result = await _vessel.GetVesselById(vesselId);
            return result.Body?.Vessel?.Vessel?.Name;
        }
        catch { return null; }
    }

    private async Task<Dictionary<long, string>> FetchProviderNamesAsync(long[] userIds, CancellationToken ct)
    {
        try
        {
            var result = await _identity.GetUserProfilesByUserIds(userIds);
            return (result.Body ?? [])
                .ToDictionary(
                    p => p.UserId,
                    p => $"{p.FirstName} {p.LastName}".Trim());
        }
        catch { return new Dictionary<long, string>(); }
    }
}
