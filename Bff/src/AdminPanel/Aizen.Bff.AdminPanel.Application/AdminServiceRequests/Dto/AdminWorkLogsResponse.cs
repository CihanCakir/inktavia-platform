using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;

[DocumentationInfo("Admin work logs response", "Work phases, log entries, provider activity and job health for a service request in the admin panel.")]
public sealed class AdminWorkLogsResponse
{
    public GetWorkLogsResponse? WorkLogs { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}
