using Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

public sealed class GetAdminServiceRequestListQuery : AizenQuery<AdminServiceRequestListResponse>
{
    public string Authorization { get; }
    public string UserToken { get; }
    public string? Status { get; }
    public long? VesselId { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public GetAdminServiceRequestListQuery(string authorization, string userToken, string? status, long? vesselId, int pageIndex, int pageSize)
    {
        Authorization = authorization;
        UserToken = userToken;
        Status = status;
        VesselId = vesselId;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
