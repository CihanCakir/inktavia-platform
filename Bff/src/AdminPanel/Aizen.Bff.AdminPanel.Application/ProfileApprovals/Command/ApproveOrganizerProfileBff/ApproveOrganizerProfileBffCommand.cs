using Aizen.Bff.AdminPanel.Application.ProfileApprovals.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ProfileApprovals.Command;

[DocumentationInfo("Approve organizer profile BFF command", "Triggers organizer profile approval via Identity admin endpoint.")]
public sealed class ApproveOrganizerProfileBffCommand : AizenCommand<ProfileApprovalDecisionBffResponse>
{
    public long UserId { get; }
    public long ProfileId { get; }

    public ApproveOrganizerProfileBffCommand(long userId, long profileId)
    {
        UserId = userId;
        ProfileId = profileId;
    }
}
