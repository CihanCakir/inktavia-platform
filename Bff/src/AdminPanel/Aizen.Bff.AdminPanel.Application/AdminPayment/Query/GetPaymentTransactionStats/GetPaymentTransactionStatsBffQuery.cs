using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetPaymentTransactionStats;

public sealed class GetPaymentTransactionStatsBffQuery : AizenQuery<GetPaymentTransactionStatsBffResponse>
{
}

public sealed class GetPaymentTransactionStatsBffResponse
{
    public PaymentTransactionStatsBffDto Stats { get; init; } = default!;
}
