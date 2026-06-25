using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;

[DocumentationInfo("Admin service request operation detail response", "Service request detail for the admin operation panel.")]
public sealed class AdminServiceRequestOperationDetailResponse
{
    public GetServiceRequestDetailResponse? ServiceRequest { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}
