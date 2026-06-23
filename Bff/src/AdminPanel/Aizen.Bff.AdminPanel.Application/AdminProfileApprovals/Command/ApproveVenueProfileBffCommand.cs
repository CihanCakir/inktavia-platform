using Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Command;

[DocumentationInfo("Approve venue profile BFF command", "Triggers venue profile approval via Identity admin endpoint.")]
public sealed class ApproveVenueProfileBffCommand : AizenCommand<ProfileApprovalDecisionBffResponse>
{
    public long UserId { get; }
    public long ProfileId { get; }
    public string UserToken { get; }

    public ApproveVenueProfileBffCommand(long userId, long profileId, string userToken)
    {
        UserId = userId;
        ProfileId = profileId;
        UserToken = userToken;
    }
}
