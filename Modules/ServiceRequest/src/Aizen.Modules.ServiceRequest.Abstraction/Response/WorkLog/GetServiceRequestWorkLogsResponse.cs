using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

[DocumentationInfo("Get work logs response", "Response containing all work log entries for an assignment.")]
public sealed class GetServiceRequestWorkLogsResponse(List<ServiceRequestWorkLogDto> workLogs)
{
    public List<ServiceRequestWorkLogDto> WorkLogs { get; } = workLogs;
}
