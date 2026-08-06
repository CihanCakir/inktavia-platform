using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;

[DocumentationInfo("Admin service request timeline response", "Service request detail used for displaying the event timeline in the admin panel.")]
public sealed class AdminServiceRequestTimelineResponse
{
    public GetServiceRequestDetailResponse? ServiceRequest { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}
