using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

[DocumentationInfo("Get admin vessel overview query handler", "Fetches the paged admin vessel list with optional search and archive filter.")]
public sealed class GetAdminVesselOverviewQueryHandler
    : AizenQueryHandler<GetAdminVesselOverviewQuery, AdminVesselOverviewResponse>
{
    private readonly IVesselAdminBffRemoteCall _vessel;

    public GetAdminVesselOverviewQueryHandler(IVesselAdminBffRemoteCall vessel)
    {
        _vessel = vessel;
    }

    public override async Task<AdminVesselOverviewResponse?> Handle(
        GetAdminVesselOverviewQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminVesselOverviewResponse();

        try
        {
            var result = await _vessel.GetAdminVesselList(
                request.Authorization,
                request.UserToken,
                request.PageIndex,
                request.PageSize,
                request.SearchTerm,
                request.IsArchived);

            response.Vessels = result.Body;
        }
        catch
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Vessel"));
        }

        return response;
    }
}
