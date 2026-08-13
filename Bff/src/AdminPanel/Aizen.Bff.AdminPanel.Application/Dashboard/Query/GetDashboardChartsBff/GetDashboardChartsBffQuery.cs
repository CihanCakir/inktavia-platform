using Aizen.Bff.AdminPanel.Application.Dashboard.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Dashboard.Query;

public sealed class GetDashboardChartsBffQuery : AizenQuery<AdminDashboardChartsResponse>
{
    public GetDashboardChartsBffQuery()
    {
    }
}
