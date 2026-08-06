using Aizen.Bff.AdminPanel.Application.ProfileApprovals.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ProfileApprovals.Query;

[DocumentationInfo("Get organizer approval detail BFF query", "Fetches full organizer profile review data for the admin approval detail screen.")]
public sealed class GetOrganizerApprovalDetailBffQuery : AizenQuery<OrganizerApprovalDetailBffResponse>
{
    public long UserId { get; }
    public long ProfileId { get; }

    public GetOrganizerApprovalDetailBffQuery(long userId, long profileId)
    {
        UserId = userId;
        ProfileId = profileId;
    }
}
