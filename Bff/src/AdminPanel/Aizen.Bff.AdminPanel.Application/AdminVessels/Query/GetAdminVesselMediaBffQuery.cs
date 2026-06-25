using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

[DocumentationInfo("Get admin vessel media BFF query", "Query for vessel media for the Media tab with optional type filter.")]
public sealed class GetAdminVesselMediaBffQuery : AizenQuery<AdminVesselMediaBffResponse>
{
    public long VesselId { get; }
    public string UserToken { get; }
    public string? MediaType { get; }
    public int PageIndex { get; }
    public int PageSize { get; }

    public GetAdminVesselMediaBffQuery(long vesselId, string userToken, string? mediaType, int pageIndex, int pageSize)
    {
        VesselId = vesselId;
        UserToken = userToken;
        MediaType = mediaType;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
