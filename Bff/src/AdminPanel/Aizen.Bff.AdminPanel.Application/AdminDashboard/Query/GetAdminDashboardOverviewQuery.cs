using Aizen.Bff.AdminPanel.Application.AdminDashboard.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminDashboard.Query;

public sealed class GetAdminDashboardOverviewQuery : AizenQuery<AdminDashboardOverviewResponse>
{
    public string Authorization { get; }
    public string UserToken { get; }
    public GetAdminDashboardOverviewQuery(string authorization, string userToken)
    {
        Authorization = authorization;
        UserToken = userToken;
    }
}
