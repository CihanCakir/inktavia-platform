using Aizen.Bff.AdminPanel.Application.AdminDashboard.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminDashboard.Query;

[DocumentationInfo("Get admin dashboard overview query handler", "Aggregates vessel counts, service request counts, open disputes and pending profile approvals from multiple downstream modules.")]
public sealed class GetAdminDashboardOverviewQueryHandler
    : AizenQueryHandler<GetAdminDashboardOverviewQuery, AdminDashboardOverviewResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly IVesselRemoteCall _vessel;
    private readonly IServiceRequestRemoteCall _serviceRequest;

    public GetAdminDashboardOverviewQueryHandler(
        IIdentityRemoteCall identity,
        IVesselRemoteCall vessel,
        IServiceRequestRemoteCall serviceRequest)
    {
        _identity = identity;
        _vessel = vessel;
        _serviceRequest = serviceRequest;
    }

    public override async Task<AdminDashboardOverviewResponse?> Handle(
        GetAdminDashboardOverviewQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminDashboardOverviewResponse();

        var vesselTask = _vessel.GetAdminVesselList();
        var srTask = _serviceRequest.GetAdminServiceRequestList();
        var disputeTask = _serviceRequest.GetAdminDisputeList();
        var orgTask = _identity.SearchOrganizerProfiles();
        var venueTask = _identity.SearchVenueProfiles();

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
