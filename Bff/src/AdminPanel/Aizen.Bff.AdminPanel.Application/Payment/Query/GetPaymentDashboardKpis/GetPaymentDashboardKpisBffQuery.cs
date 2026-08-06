using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPaymentDashboardKpis;

public sealed class GetPaymentDashboardKpisBffQuery : AizenQuery<GetPaymentDashboardKpisBffResponse>
{
}

public sealed class GetPaymentDashboardKpisBffResponse
{
    public PaymentDashboardKpisBffDto Kpis { get; init; } = default!;
}
