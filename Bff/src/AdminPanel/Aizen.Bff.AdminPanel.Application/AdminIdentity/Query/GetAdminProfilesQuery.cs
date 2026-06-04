using Aizen.Bff.AdminPanel.Application.AdminIdentity.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminIdentity.Query;

public sealed class GetAdminProfilesQuery : AizenQuery<AdminUserOverviewResponse>
{
    public string Authorization { get; }
    public string UserToken { get; }
    public string? RoleContext { get; }
    public string? ApprovalStatus { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public GetAdminProfilesQuery(string authorization, string userToken, string? roleContext, string? approvalStatus, int pageIndex, int pageSize)
    {
        Authorization = authorization;
        UserToken = userToken;
        RoleContext = roleContext;
        ApprovalStatus = approvalStatus;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
