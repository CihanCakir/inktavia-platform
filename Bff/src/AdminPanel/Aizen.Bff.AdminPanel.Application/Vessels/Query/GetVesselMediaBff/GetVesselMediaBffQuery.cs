using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Query;

[DocumentationInfo("Get admin vessel media BFF query", "Query for vessel media for the Media tab with optional type filter.")]
public sealed class GetVesselMediaBffQuery : AizenQuery<AdminVesselMediaBffResponse>
{
    public long VesselId { get; }
    public string? MediaType { get; }
    public int PageIndex { get; }
    public int PageSize { get; }

    public GetVesselMediaBffQuery(long vesselId, string? mediaType, int pageIndex, int pageSize)
    {
        VesselId = vesselId;
        MediaType = mediaType;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
