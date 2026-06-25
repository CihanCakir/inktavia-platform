using Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Command;

[DocumentationInfo("Approve organizer profile BFF command", "Triggers organizer profile approval via Identity admin endpoint.")]
public sealed class ApproveOrganizerProfileBffCommand : AizenCommand<ProfileApprovalDecisionBffResponse>
{
    public long UserId { get; }
    public long ProfileId { get; }
    public string UserToken { get; }

    public ApproveOrganizerProfileBffCommand(long userId, long profileId, string userToken)
    {
        UserId = userId;
        ProfileId = profileId;
        UserToken = userToken;
    }
}
