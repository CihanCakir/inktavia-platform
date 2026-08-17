using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

public sealed class GetAdminServiceRequestListQuery : AizenQuery<AdminServiceRequestListResponse>
{
    public string? Status { get; }
    public long? VesselId { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public GetAdminServiceRequestListQuery(string? status, long? vesselId, int pageIndex, int pageSize)
    {
        Status = status;
        VesselId = vesselId;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
