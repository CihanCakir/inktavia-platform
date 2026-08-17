using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPaymentTransactionStats;

public sealed class GetPaymentTransactionStatsBffQuery : AizenQuery<GetPaymentTransactionStatsBffResponse>
{
}

public sealed class GetPaymentTransactionStatsBffResponse
{
    public PaymentTransactionStatsBffDto Stats { get; init; } = default!;
}
