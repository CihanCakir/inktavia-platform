using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetPaymentDashboardKpis;

public sealed class GetPaymentDashboardKpisBffQuery : AizenQuery<GetPaymentDashboardKpisBffResponse>
{
}

public sealed class GetPaymentDashboardKpisBffResponse
{
    public PaymentDashboardKpisBffDto Kpis { get; init; } = default!;
}
