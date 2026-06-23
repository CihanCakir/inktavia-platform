using Aizen.Bff.AdminPanel.Application.AdminUsers.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminUsers.Query;

[DocumentationInfo("Get admin user list BFF query", "Query for the User Management List with filters, pagination, and UI-enriched response.")]
public sealed class GetAdminUserListBffQuery : AizenQuery<AdminUserListBffResponse>
{
    public string UserToken { get; }
    public string? Search { get; }
    public string? Role { get; }
    public string? Status { get; }
    public string? IdentityType { get; }
    public string? RegisteredFrom { get; }
    public string? RegisteredTo { get; }
    public int Page { get; }
    public int PageSize { get; }

    public GetAdminUserListBffQuery(
        string userToken,
        string? search,
        string? role,
        string? status,
        string? identityType,
        string? registeredFrom,
        string? registeredTo,
        int page,
        int pageSize)
    {
        UserToken = userToken;
        Search = search;
        Role = role;
        Status = status;
        IdentityType = identityType;
        RegisteredFrom = registeredFrom;
        RegisteredTo = registeredTo;
        Page = page;
        PageSize = pageSize;
    }
}
