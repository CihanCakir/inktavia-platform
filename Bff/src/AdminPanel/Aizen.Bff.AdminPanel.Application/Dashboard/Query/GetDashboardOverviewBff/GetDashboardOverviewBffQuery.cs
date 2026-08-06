using Aizen.Bff.AdminPanel.Application.AdminDashboard.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminDashboard.Query;

public sealed class GetAdminDashboardOverviewQuery : AizenQuery<AdminDashboardOverviewResponse>
{
    public GetAdminDashboardOverviewQuery()
    {
    }
}
