using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

[DocumentationInfo("Add work log response", "Response after provider adds a work log entry.")]
public sealed class AddServiceRequestWorkLogResponse(ServiceRequestWorkLogDto workLog)
{
    public ServiceRequestWorkLogDto WorkLog { get; } = workLog;
}
