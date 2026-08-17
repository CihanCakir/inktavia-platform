using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetInvoicesPaged;

[DocumentationInfo("Get invoices paged query handler",
    "Admin-facing paged invoice list with optional filters for type, status, buyer, date range, and search.")]
public sealed class GetInvoicesPagedQueryHandler
    : AizenQueryHandler<GetInvoicesPagedQuery, InvoiceListResult>
{
    private readonly IInvoiceRepository _invoices;

    public GetInvoicesPagedQueryHandler(IInvoiceRepository invoices)
        => _invoices = invoices;

    public override async Task<InvoiceListResult?> Handle(
        GetInvoicesPagedQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;
        var (items, total) = await _invoices.GetPagedAsync(
            request.Type, request.Status, request.Prefix,
            request.BuyerUserId, request.FromDate, request.ToDate,
            request.Search, skip, request.PageSize, ct);

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
