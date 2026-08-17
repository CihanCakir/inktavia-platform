using Aizen.Bff.AdminPanel.Application.Users.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Users.Query;

[DocumentationInfo("Get admin user detail BFF query", "Fetches full user detail for the admin user detail page.")]
public sealed class GetUserDetailBffQuery : AizenQuery<AdminUserDetailBffResponse>
{
    public long ProfileId { get; }

    public GetUserDetailBffQuery(long profileId)
    {
        ProfileId = profileId;
    }
}
