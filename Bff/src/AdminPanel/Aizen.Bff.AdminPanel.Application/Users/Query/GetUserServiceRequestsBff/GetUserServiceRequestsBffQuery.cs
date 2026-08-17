using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Users.Query;

[DocumentationInfo("Get admin user service requests BFF query", "Returns service requests scoped to a specific user (ownerUserId) for the User Detail service-requests tab.")]
public sealed class GetUserServiceRequestsBffQuery : AizenQuery<AdminServiceRequestListResponse>
{
    public long ProfileId { get; }
    public string? Status  { get; }
    public int     Page    { get; }
    public int     PageSize { get; }

    public GetUserServiceRequestsBffQuery(long profileId, string? status, int page, int pageSize)
    {
        ProfileId = profileId;
        Status    = status;
        Page      = page;
        PageSize  = pageSize;
    }
}
