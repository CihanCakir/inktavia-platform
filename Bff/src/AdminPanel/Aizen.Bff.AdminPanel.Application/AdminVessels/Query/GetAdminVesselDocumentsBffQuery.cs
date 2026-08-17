using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

[DocumentationInfo("Get admin vessel documents BFF query", "Query for vessel documents with computed expiry status for the Documents tab.")]
public sealed class GetAdminVesselDocumentsBffQuery : AizenQuery<AdminVesselDocumentsBffResponse>
{
    public long VesselId { get; }
    public string? StatusFilter { get; }

    public GetAdminVesselDocumentsBffQuery(long vesselId, string? statusFilter = null)
    {
        VesselId = vesselId;
        StatusFilter = statusFilter;
    }
}
