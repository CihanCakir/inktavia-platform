using Aizen.Bff.AdminPanel.Application.AdminDashboard.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminDashboard.Query;

public sealed class GetAdminDashboardOverviewQuery : AizenQuery<AdminDashboardOverviewResponse>
{
    public string UserToken { get; }
    public GetAdminDashboardOverviewQuery(string userToken)
    {
        UserToken = userToken;
    }
}
