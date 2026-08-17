using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Modules.Vessel.Abstraction.Response.Document;
using Aizen.Modules.Vessel.Abstraction.Response.Ownership;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Dto;

[DocumentationInfo("Admin vessel documents response", "Aggregated vessel detail, documents and owners for the admin vessel detail screen.")]
public sealed class AdminVesselDocumentsResponse
{
    public GetVesselDetailResponse? Vessel { get; set; }
    public GetVesselDocumentsResponse? Documents { get; set; }
    public GetVesselOwnersResponse? Owners { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}
