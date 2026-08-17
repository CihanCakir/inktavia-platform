using Aizen.Bff.AdminPanel.Application.Users.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Users.Query;

[DocumentationInfo("Get admin user vessels BFF query", "Returns the vessel list for a specific user's vessels tab.")]
public sealed class GetUserVesselsBffQuery : AizenQuery<AdminUserVesselsBffResponse>
{
    public long ProfileId { get; }

    public GetUserVesselsBffQuery(long profileId)
    {
        ProfileId = profileId;
    }
}
