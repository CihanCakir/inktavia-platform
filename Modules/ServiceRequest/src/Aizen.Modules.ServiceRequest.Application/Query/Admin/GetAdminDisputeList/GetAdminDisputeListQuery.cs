using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;

namespace Aizen.Modules.ServiceRequest.Application.Query.Admin;

[DocumentationInfo("Admin dispute list query", "Returns paginated open dispute list for admin.")]
public sealed class GetAdminDisputeListQuery : AizenQuery<GetAdminDisputeListResponse>
{
    public AdminDisputeFilterRequest Filter { get; }
    public GetAdminDisputeListQuery(AdminDisputeFilterRequest filter) => Filter = filter;
}
