using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPaymentRefundHistory;

public sealed class GetPaymentRefundHistoryBffQuery : AizenQuery<GetPaymentRefundHistoryBffResponse>
{
    public long TransactionId { get; init; }
}

public sealed class GetPaymentRefundHistoryBffResponse
{
    public List<TransactionRefundRecordBffDto> Records { get; init; } = [];
}
