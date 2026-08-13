using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;

namespace Aizen.Modules.ServiceRequest.Application.Query.Admin;

[DocumentationInfo("Admin service request status breakdown query",
    "Read-only per-status counts for the admin dashboard SR-volume chart (C2).")]
public sealed class GetAdminServiceRequestStatusBreakdownQuery : AizenQuery<List<ServiceRequestStatusCountDto>>
{
}
