using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;

namespace Aizen.Modules.ServiceRequest.Application.Query.Admin;

[DocumentationInfo("Admin service request stats query", "Read-only KPI counts for the admin service-request list.")]
public sealed class GetAdminServiceRequestStatsQuery : AizenQuery<GetAdminServiceRequestStatsResponse>
{
}
