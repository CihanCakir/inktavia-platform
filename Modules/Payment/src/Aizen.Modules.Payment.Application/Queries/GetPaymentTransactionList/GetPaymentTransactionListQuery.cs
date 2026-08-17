using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Queries.GetPaymentTransaction;

namespace Aizen.Modules.Payment.Application.Queries.GetPaymentTransactionList;

public sealed class GetPaymentTransactionListQuery : AizenQuery<PaymentTransactionListResult>
{
    public PaymentTransactionStatus? Status       { get; init; }
    public TransactionType?          Type         { get; init; }
    public string?                   Gateway      { get; init; }
    public DateTime?                 FromDate     { get; init; }
    public DateTime?                 ToDate       { get; init; }
    public string?                   Search       { get; init; }
    public int                       Page         { get; init; } = 1;
    public int                       PageSize     { get; init; } = 25;
}

public sealed record PaymentTransactionListResult(
    List<PaymentTransactionDto> Items,
    int Total,
    int Page,
    int PageSize
);
