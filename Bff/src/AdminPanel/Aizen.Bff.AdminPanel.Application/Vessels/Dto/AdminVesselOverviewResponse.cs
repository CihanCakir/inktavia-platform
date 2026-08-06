using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Dto;

[DocumentationInfo("Admin vessel overview response", "Paged vessel list with warnings for the admin vessels screen.")]
public sealed class AdminVesselOverviewResponse
{
    public GetAllVesselsAdminResponse? Vessels { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}
