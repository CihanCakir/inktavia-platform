using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Query;

public sealed class GetAdminDisputeListQuery : AizenQuery<GetAdminDisputeListResponse>
{
    public string Authorization { get; }
    public string UserToken { get; }
    public string? Status { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public GetAdminDisputeListQuery(string authorization, string userToken, string? status, int pageIndex, int pageSize)
    {
        Authorization = authorization;
        UserToken = userToken;
        Status = status;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
