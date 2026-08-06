using Aizen.Bff.AdminPanel.Application.ProfileApprovals.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ProfileApprovals.Command;

[DocumentationInfo("Reject venue profile BFF command", "Triggers venue profile rejection via Identity admin endpoint with a required reason.")]
public sealed class RejectVenueProfileBffCommand : AizenCommand<ProfileApprovalDecisionBffResponse>
{
    public long UserId { get; }
    public long ProfileId { get; }
    public string Reason { get; }

    public RejectVenueProfileBffCommand(long userId, long profileId, string reason)
    {
        UserId = userId;
        ProfileId = profileId;
        Reason = reason;
    }
}
