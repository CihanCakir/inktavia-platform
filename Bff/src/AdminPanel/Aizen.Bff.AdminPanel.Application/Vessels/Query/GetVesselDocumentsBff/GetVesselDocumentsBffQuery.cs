using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Query;

[DocumentationInfo("Get admin vessel documents BFF query", "Query for vessel documents with computed expiry status for the Documents tab.")]
public sealed class GetVesselDocumentsBffQuery : AizenQuery<AdminVesselDocumentsBffResponse>
{
    public long VesselId { get; }
    public string? StatusFilter { get; }

    public GetVesselDocumentsBffQuery(long vesselId, string? statusFilter = null)
    {
        VesselId = vesselId;
        StatusFilter = statusFilter;
    }
}
