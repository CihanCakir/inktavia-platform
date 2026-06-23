using Aizen.Bff.AdminPanel.Application.Common.Warnings;

namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;

[DocumentationInfo("Admin profile approval queue BFF response", "Combined organizer+venue pending queue with pagination, summary KPIs and warnings.")]
public sealed class AdminProfileApprovalQueueBffResponse
{
    public ApprovalQueuePageBffDto? Approvals { get; set; }
    public ProfileApprovalSummaryBffDto Summary { get; set; } = new();
    public List<AdminBffWarning> Warnings { get; set; } = new();
}

[DocumentationInfo("Approval queue page BFF DTO", "Pagination wrapper for the merged organizer+venue approval queue.")]
public sealed class ApprovalQueuePageBffDto
{
    public int From { get; set; }
    public int Index { get; set; }
    public int Size { get; set; }
    public long Count { get; set; }
    public int Pages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
    public List<ProfileApprovalQueueItemBffDto> Items { get; set; } = new();
}
