using Aizen.Bff.AdminPanel.Application.AdminPayment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetPaymentRefundHistory;

public sealed class GetPaymentRefundHistoryBffQuery : AizenQuery<GetPaymentRefundHistoryBffResponse>
{
    public long TransactionId { get; init; }
}

public sealed class GetPaymentRefundHistoryBffResponse
{
    public List<TransactionRefundRecordBffDto> Records { get; init; } = [];
}
