using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;

namespace Aizen.Modules.ServiceRequest.Application.Query.Admin;

[DocumentationInfo("Admin service request list query", "Returns paginated service request list with admin-level filters.")]
public sealed class GetAdminServiceRequestListQuery : AizenQuery<GetAdminServiceRequestListResponse>
{
    public AdminServiceRequestFilterRequest Filter { get; }
    public GetAdminServiceRequestListQuery(AdminServiceRequestFilterRequest filter) => Filter = filter;
}
