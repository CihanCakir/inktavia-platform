using Aizen.Bff.AdminPanel.Application.Users.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Users.Query;

[DocumentationInfo("Get admin user KPI BFF query", "Returns aggregate user counts for the KPI bar: total, active today, pending, suspended.")]
public sealed class GetUserKpiBffQuery : AizenQuery<AdminUserKpiBffResponse>
{
    public GetUserKpiBffQuery() { }
}
