using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Bff.AdminPanel.Application.AdminPayment.Query.GetInvoiceFull;

public sealed class GetInvoiceFullBffQuery : AizenQuery<GetInvoiceFullBffResponse>
{
    public long Id { get; init; }
}

public sealed class GetInvoiceFullBffResponse
{
    public InvoiceDto? Invoice { get; init; }
}
