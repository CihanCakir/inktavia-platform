using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetInvoiceDetail;

public sealed class GetInvoiceDetailBffQuery : AizenQuery<GetInvoiceDetailBffResponse>
{
    public long Id { get; init; }
}

public sealed class GetInvoiceDetailBffResponse
{
    public InvoiceDto? Invoice { get; init; }
}
