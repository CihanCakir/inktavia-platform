using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Request.WorkLog;

[DocumentationInfo("Add work log request", "Provider adds a work log entry to an assignment.")]
public sealed class AddServiceRequestWorkLogRequest
{
    public ServiceRequestWorkLogType LogType { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public decimal? LocationLatitude { get; set; }
    public decimal? LocationLongitude { get; set; }
    public Guid? AttachmentFileId { get; set; }
}
