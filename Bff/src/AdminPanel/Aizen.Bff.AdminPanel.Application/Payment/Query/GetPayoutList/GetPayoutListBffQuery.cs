using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPayoutList;

public sealed class GetPayoutListBffQuery : AizenQuery<GetPayoutListBffResponse>
{
    public string? Status   { get; init; }
    public int     Page     { get; init; } = 1;
    public int     PageSize { get; init; } = 25;
}

public sealed class GetPayoutListBffResponse
{
    public PaymentPayoutListBffResult Result { get; init; } = default!;
}
