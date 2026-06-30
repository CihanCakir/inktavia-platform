using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetPaymentTransactionDetail;

public sealed class GetPaymentTransactionDetailBffQuery : AizenQuery<GetPaymentTransactionDetailBffResponse>
{
    public long Id { get; init; }
}

public sealed class GetPaymentTransactionDetailBffResponse
{
    public PaymentTransactionBffDto? Transaction { get; init; }
}
