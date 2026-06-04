using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Message;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

public sealed class GetAdminVesselOverviewQuery : AizenQuery<AdminVesselOverviewResponse>
{
    public string Authorization { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public string? SearchTerm { get; }
    public bool? IsArchived { get; }
    public GetAdminVesselOverviewQuery(string authorization, int pageIndex, int pageSize, string? searchTerm, bool? isArchived)
    {
        Authorization = authorization;
        PageIndex = pageIndex;
        PageSize = pageSize;
        SearchTerm = searchTerm;
        IsArchived = isArchived;
    }
}

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
