using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;

[DocumentationInfo("Admin service request filter options response", "Static filter option lists for service request status and dispute status dropdowns.")]
public sealed class AdminServiceRequestFilterOptionsResponse
{
    public List<string>? StatusOptions { get; set; }
    public List<string>? DisputeStatusOptions { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}
