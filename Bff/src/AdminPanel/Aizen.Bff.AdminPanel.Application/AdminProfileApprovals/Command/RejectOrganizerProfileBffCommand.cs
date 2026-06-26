using Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Command;

[DocumentationInfo("Reject organizer profile BFF command", "Triggers organizer profile rejection via Identity admin endpoint with a required reason.")]
public sealed class RejectOrganizerProfileBffCommand : AizenCommand<ProfileApprovalDecisionBffResponse>
{
    public long UserId { get; }
    public long ProfileId { get; }
    public string Reason { get; }

    public RejectOrganizerProfileBffCommand(long userId, long profileId, string reason)
    {
        UserId = userId;
        ProfileId = profileId;
        Reason = reason;
    }
}
