using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetInvoicesByBuyer;

public sealed class GetInvoicesByBuyerBffQuery : AizenQuery<GetInvoicesByBuyerBffResponse>
{
    public long BuyerId  { get; init; }
    public int  Page     { get; init; } = 1;
    public int  PageSize { get; init; } = 25;
}

public sealed class GetInvoicesByBuyerBffResponse
{
    public InvoiceListResult Result { get; init; } = default!;
}
