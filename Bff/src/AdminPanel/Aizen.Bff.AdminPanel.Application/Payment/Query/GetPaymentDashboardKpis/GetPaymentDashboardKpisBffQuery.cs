using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPaymentDashboardKpis;

public sealed class GetPaymentDashboardKpisBffQuery : AizenQuery<GetPaymentDashboardKpisBffResponse>
{
    // FIX_DASHBOARD_KPI_PERIOD: 1W | 1M | 1Y (default 1M in the module handler)
    public string? Period { get; init; }
}

public sealed class GetPaymentDashboardKpisBffResponse
{
    public PaymentDashboardKpisBffDto Kpis { get; init; } = default!;
}
