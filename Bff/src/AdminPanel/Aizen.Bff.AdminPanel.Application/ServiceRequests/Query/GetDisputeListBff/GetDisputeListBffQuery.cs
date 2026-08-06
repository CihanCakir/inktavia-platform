using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

public sealed class GetDisputeListBffQuery : AizenQuery<GetAdminDisputeListResponse>
{
    public string? Status { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public GetDisputeListBffQuery(string? status, int pageIndex, int pageSize)
    {
        Status = status;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
