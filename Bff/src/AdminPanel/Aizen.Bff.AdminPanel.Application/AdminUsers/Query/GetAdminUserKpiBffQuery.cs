using Aizen.Bff.AdminPanel.Application.AdminUsers.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminUsers.Query;

[DocumentationInfo("Get admin user KPI BFF query", "Returns aggregate user counts for the KPI bar: total, active today, pending, suspended.")]
public sealed class GetAdminUserKpiBffQuery : AizenQuery<AdminUserKpiBffResponse>
{
    public GetAdminUserKpiBffQuery() { }
}
