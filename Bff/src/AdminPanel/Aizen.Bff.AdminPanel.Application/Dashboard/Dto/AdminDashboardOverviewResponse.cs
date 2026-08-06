using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;

namespace Aizen.Bff.AdminPanel.Application.AdminDashboard.Dto;

[DocumentationInfo("Admin dashboard overview response", "Aggregated metrics for the admin dashboard overview screen.")]
public sealed class AdminDashboardOverviewResponse
{
    public int TotalVessels { get; set; }
    public int TotalActiveServiceRequests { get; set; }
    public int TotalOpenDisputes { get; set; }
    public int PendingOrganizerApprovals { get; set; }
    public int PendingVenueApprovals { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}
