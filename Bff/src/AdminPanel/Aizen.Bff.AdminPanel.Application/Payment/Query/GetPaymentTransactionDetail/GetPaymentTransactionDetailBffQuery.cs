using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPaymentTransactionDetail;

public sealed class GetPaymentTransactionDetailBffQuery : AizenQuery<GetPaymentTransactionDetailBffResponse>
{
    public long Id { get; init; }
}

public sealed class GetPaymentTransactionDetailBffResponse
{
    public PaymentTransactionBffDto? Transaction { get; init; }
}
