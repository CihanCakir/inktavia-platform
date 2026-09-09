using Aizen.Bff.AdminPanel.Application.Dashboard.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Dashboard.Query;

[DocumentationInfo("Get admin dashboard overview query handler", "Aggregates vessel counts, service request counts, open disputes and pending profile approvals from multiple downstream modules.")]
public sealed class GetDashboardOverviewBffQueryHandler
    : AizenQueryHandler<GetDashboardOverviewBffQuery, AdminDashboardOverviewResponse>, IAizenQueryHandlerCacheable
{
    // #107: this endpoint fans out to 5 downstream modules. Under load its p90–p95 tail scaled worse than
    // sibling endpoints because every concurrent request repeated the full fan-out. A short-TTL server-side
    // cache collapses N concurrent requests into ONE fan-out per window; the ~5s freshness lag is acceptable
    // for a dashboard summary.
    private static readonly TimeSpan OverviewCacheTtl = TimeSpan.FromSeconds(20);

    private readonly IIdentityRemoteCall _identity;
    private readonly IVesselRemoteCall _vessel;
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public GetDashboardOverviewBffQueryHandler(
        IIdentityRemoteCall identity,
        IVesselRemoteCall vessel,
        IServiceRequestRemoteCall serviceRequest)
    {
        _identity = identity;
        _vessel = vessel;
        _serviceRequest = serviceRequest;
    }

    // Distributed so the whole replica set shares one fan-out per window (and reports identical numbers).
    public AizenCacheType CacheType => AizenCacheType.Distributed;

    // The overview payload is language-INDEPENDENT: it is integer counts plus warnings whose text is a
    // constant module identifier (e.g. "Vessel"), never a localized string. So the language-blind
    // AizenQueryCacheKey (known debt D-05) is safe here — there is nothing locale-specific to key on.
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = OverviewCacheTtl };

    public override async Task<AdminDashboardOverviewResponse?> Handle(
        GetDashboardOverviewBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminDashboardOverviewResponse();

        // Only the total counts are consumed, so request the smallest page (pageSize: 1) instead of the
        // default 20 rich DTOs per list — the paged Count/TotalCount is the total regardless of page size.
        var vesselTask = _vessel.GetAdminVesselList(pageSize: 1);
        var srTask = _serviceRequest.GetAdminServiceRequestList(pageSize: 1);
        var disputeTask = _serviceRequest.GetAdminDisputeList(pageSize: 1);
        var orgTask = _identity.SearchOrganizerProfiles(pageSize: 1);
        var venueTask = _identity.SearchVenueProfiles(pageSize: 1);

        await Task.WhenAll(
            vesselTask.ContinueWith(_ => { }),
            srTask.ContinueWith(_ => { }),
            disputeTask.ContinueWith(_ => { }),
            orgTask.ContinueWith(_ => { }),
            venueTask.ContinueWith(_ => { }));

        if (vesselTask.IsCompletedSuccessfully)
            response.TotalVessels = vesselTask.Result.Body?.Vessels?.Count ?? 0;
        else
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Vessel"));

        if (srTask.IsCompletedSuccessfully)
            response.TotalActiveServiceRequests = srTask.Result.Body?.TotalCount ?? 0;
        else
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ServiceRequest"));

        if (disputeTask.IsCompletedSuccessfully)
            response.TotalOpenDisputes = disputeTask.Result.Body?.TotalCount ?? 0;
        else
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ServiceRequest.Disputes"));

        if (orgTask.IsCompletedSuccessfully)
            response.PendingOrganizerApprovals = orgTask.Result.Body?.TotalCount ?? 0;
        else
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity.Organizers"));

        if (venueTask.IsCompletedSuccessfully)
            response.PendingVenueApprovals = venueTask.Result.Body?.TotalCount ?? 0;
        else
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity.Venues"));

        return response;
    }
}
