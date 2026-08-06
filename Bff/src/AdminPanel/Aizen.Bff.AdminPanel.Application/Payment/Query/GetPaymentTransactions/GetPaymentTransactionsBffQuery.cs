using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPaymentTransactions;

public sealed class GetPaymentTransactionsBffQuery : AizenQuery<GetPaymentTransactionsBffResponse>
{
    public PaymentTransactionStatus? Status   { get; init; }
    public TransactionType?          Type     { get; init; }
    public string?                   Gateway  { get; init; }
    public DateTime?                 FromDate { get; init; }
    public DateTime?                 ToDate   { get; init; }
    public string?                   Search   { get; init; }
    public int                       Page     { get; init; } = 1;
    public int                       PageSize { get; init; } = 25;
}

public sealed class GetPaymentTransactionsBffResponse
{
    public PaymentTransactionListBffResult Result { get; init; } = default!;
}
