using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Queries.GetInvoicesPaged;

/// <summary>Admin paged invoice list with optional filters.</summary>
public sealed class GetInvoicesPagedQuery : AizenQuery<InvoiceListResult>
{
    public InvoiceType?   Type       { get; init; }
    public InvoiceStatus? Status     { get; init; }
    public string?        Prefix     { get; init; }
    public long?          BuyerUserId { get; init; }
    public DateTime?      FromDate   { get; init; }
    public DateTime?      ToDate     { get; init; }
    public string?        Search     { get; init; }
    public int            Page       { get; init; } = 1;
    public int            PageSize   { get; init; } = 25;
}
