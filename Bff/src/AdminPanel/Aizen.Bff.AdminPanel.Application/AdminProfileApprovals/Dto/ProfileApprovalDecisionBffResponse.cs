using Aizen.Bff.AdminPanel.Application.Common.Warnings;

namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;

[DocumentationInfo("Profile approval decision BFF response", "Response after approving or rejecting an organizer/venue profile.")]
public sealed class ProfileApprovalDecisionBffResponse
{
    public ProfileApprovalDecisionBffDto? Decision { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}

[DocumentationInfo("Profile approval decision BFF DTO", "Decision result with userId, profileId, type, and outcome status.")]
public sealed class ProfileApprovalDecisionBffDto
{
    public long UserId { get; set; }
    public long ProfileId { get; set; }
    public string ProfileType { get; set; } = null!;     // "organizer" | "venue"
    public string Status { get; set; } = null!;          // "approved" | "rejected"
    public string? ReviewedAt { get; set; }              // Gap: not returned by Identity EmptyResult
    public string? ReviewedByName { get; set; }          // Gap: not returned by Identity EmptyResult
}
