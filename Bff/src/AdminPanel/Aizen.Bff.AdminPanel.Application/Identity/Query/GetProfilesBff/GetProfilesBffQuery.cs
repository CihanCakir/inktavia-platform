using Aizen.Bff.AdminPanel.Application.Identity.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Identity.Query;

public sealed class GetProfilesBffQuery : AizenQuery<AdminUserOverviewResponse>
{
    public string? RoleContext { get; }
    public string? ApprovalStatus { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public GetProfilesBffQuery(string? roleContext, string? approvalStatus, int pageIndex, int pageSize)
    {
        RoleContext = roleContext;
        ApprovalStatus = approvalStatus;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
