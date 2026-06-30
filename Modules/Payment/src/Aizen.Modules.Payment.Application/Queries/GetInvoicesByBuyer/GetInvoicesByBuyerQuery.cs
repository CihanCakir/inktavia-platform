using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Queries.GetInvoicesByBuyer;

/// <summary>
/// Returns a paged invoice list scoped to a specific buyer.
/// Used by admin buyer detail pages and future self-service buyer portal.
/// </summary>
public sealed class GetInvoicesByBuyerQuery : AizenQuery<InvoiceListResult>
{
    public required long          BuyerUserId { get; init; }
    public          InvoiceStatus? Status     { get; init; }
    public          InvoiceType?   Type       { get; init; }
    public          int            Page       { get; init; } = 1;
    public          int            PageSize   { get; init; } = 25;
}
