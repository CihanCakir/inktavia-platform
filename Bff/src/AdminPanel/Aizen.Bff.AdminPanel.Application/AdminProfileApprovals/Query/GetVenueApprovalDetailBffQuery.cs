using Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Query;

[DocumentationInfo("Get venue approval detail BFF query", "Fetches full venue profile review data for the admin approval detail screen.")]
public sealed class GetVenueApprovalDetailBffQuery : AizenQuery<VenueApprovalDetailBffResponse>
{
    public long UserId { get; }
    public long ProfileId { get; }

    public GetVenueApprovalDetailBffQuery(long userId, long profileId)
    {
        UserId = userId;
        ProfileId = profileId;
    }
}
