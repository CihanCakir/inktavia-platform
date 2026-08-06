using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetInvoicesPaged;

public sealed class GetInvoicesPagedBffQuery : AizenQuery<GetInvoicesPagedBffResponse>
{
    public int     Page     { get; init; } = 1;
    public int     PageSize { get; init; } = 25;
    public string? Status   { get; init; }
}

public sealed class GetInvoicesPagedBffResponse
{
    public InvoiceListResult Result { get; init; } = default!;
}
