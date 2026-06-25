using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

[DocumentationInfo("Add work log entry response", "Response after adding a new work log entry.")]
public sealed class AddWorkLogEntryResponse(WorkLogEntryItemDto entry)
{
    public WorkLogEntryItemDto Entry { get; } = entry;
}
