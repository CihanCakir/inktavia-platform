namespace Aizen.Bff.AdminPanel.Application.ProfileApprovals.Dto;

[DocumentationInfo("Reject profile approval BFF request", "Frontend request body for rejecting an organizer or venue profile. Only Reason is forwarded to Identity; the other fields are BFF-only (gap: Identity does not persist them).")]
public sealed class RejectProfileApprovalBffRequest
{
    public string Reason { get; set; } = null!;      // required, 10–1000 chars
    public string? ReasonCategory { get; set; }      // Gap: Identity does not accept this
    public string? InternalNote { get; set; }         // Gap: Identity does not accept this
    public bool NotifyUser { get; set; } = true;     // Gap: Identity does not accept this
}
