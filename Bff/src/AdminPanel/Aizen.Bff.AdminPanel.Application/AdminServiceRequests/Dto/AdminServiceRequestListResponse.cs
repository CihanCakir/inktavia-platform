using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;

[DocumentationInfo("Admin service request list response", "Paged service request list for the admin service requests screen. Uses BFF-owned DTO to avoid interface serialization issues.")]
public sealed class AdminServiceRequestListResponse
{
    public ServiceRequestPageBffDto? ServiceRequests { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}
