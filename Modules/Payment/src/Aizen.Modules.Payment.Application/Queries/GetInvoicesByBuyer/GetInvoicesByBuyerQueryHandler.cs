using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetInvoicesByBuyer;

[DocumentationInfo("Get invoices by buyer query handler",
    "Returns a paged invoice list filtered to a specific BuyerUserId with optional status and type filters.")]
public sealed class GetInvoicesByBuyerQueryHandler
    : AizenQueryHandler<GetInvoicesByBuyerQuery, InvoiceListResult>
{
    private readonly IInvoiceRepository _invoices;

    public GetInvoicesByBuyerQueryHandler(IInvoiceRepository invoices)
        => _invoices = invoices;

    public override async Task<InvoiceListResult?> Handle(
        GetInvoicesByBuyerQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;
        var (items, total) = await _invoices.GetByBuyerPagedAsync(
            request.BuyerUserId, request.Status, request.Type,
            skip, request.PageSize, ct);

        var dtos = items.Select(inv => new InvoiceListItemDto(
            inv.Id,
            inv.InvoiceNumber,
            inv.InvoiceType,
            inv.Status,
            inv.BuyerUserId,
            inv.BuyerName,
            inv.Currency,
            inv.TotalAmount,
            inv.PaidAmount,
            inv.RemainingAmount,
            inv.IssueDateUtc,
            inv.DueDateUtc,
            inv.CreateDate)).ToList();

        return new InvoiceListResult(dtos, total, request.Page, request.PageSize);
    }
}
