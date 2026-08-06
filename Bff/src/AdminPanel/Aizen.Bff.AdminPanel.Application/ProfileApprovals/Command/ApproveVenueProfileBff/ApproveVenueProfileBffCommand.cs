using Aizen.Bff.AdminPanel.Application.ProfileApprovals.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ProfileApprovals.Command;

[DocumentationInfo("Approve venue profile BFF command", "Triggers venue profile approval via Identity admin endpoint.")]
public sealed class ApproveVenueProfileBffCommand : AizenCommand<ProfileApprovalDecisionBffResponse>
{
    public long UserId { get; }
    public long ProfileId { get; }

    public ApproveVenueProfileBffCommand(long userId, long profileId)
    {
        UserId = userId;
        ProfileId = profileId;
    }
}
