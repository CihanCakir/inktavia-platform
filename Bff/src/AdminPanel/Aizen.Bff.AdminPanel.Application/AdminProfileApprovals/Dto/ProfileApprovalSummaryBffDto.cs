namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;

[DocumentationInfo("Profile approval summary BFF DTO", "Aggregate KPI counters for the approval queue dashboard header.")]
public sealed class ProfileApprovalSummaryBffDto
{
    public int PendingOrganizers { get; set; }
    public int PendingVenues { get; set; }
    public int ApprovedThisWeek { get; set; }    // Gap: computed from current page only
    public int RejectedThisWeek { get; set; }    // Gap: computed from current page only
    public double? AverageReviewTimeHours { get; set; }  // Gap: not available from Identity
    public int IncompleteOrganizers { get; set; }
    public int NeedsRevisionOrganizers { get; set; }
}
