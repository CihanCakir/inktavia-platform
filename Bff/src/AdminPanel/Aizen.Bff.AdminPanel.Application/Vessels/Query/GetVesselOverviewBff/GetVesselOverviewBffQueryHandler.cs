using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Query;

[DocumentationInfo("Get admin vessel overview query handler", "Fetches the paged admin vessel list with optional search and archive filter.")]
public sealed class GetVesselOverviewBffQueryHandler
    : AizenQueryHandler<GetVesselOverviewBffQuery, AdminVesselOverviewResponse>
{
    private readonly IVesselRemoteCall _vessel;

    public GetVesselOverviewBffQueryHandler(IVesselRemoteCall vessel)
    {
        _vessel = vessel;
    }

    public override async Task<AdminVesselOverviewResponse?> Handle(
        GetVesselOverviewBffQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminVesselOverviewResponse();

        try
        {

        var result = await _vessel.GetAdminVesselList(
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
