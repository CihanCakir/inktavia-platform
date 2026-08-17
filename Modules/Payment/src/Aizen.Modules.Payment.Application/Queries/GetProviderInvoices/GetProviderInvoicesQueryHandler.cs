using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderInvoices;

public sealed class GetProviderInvoicesQueryHandler
    : AizenQueryHandler<GetProviderInvoicesQuery, ProviderInvoicePagedResultDto>
{
    private readonly IInvoiceRepository _repo;

    public GetProviderInvoicesQueryHandler(IInvoiceRepository repo) => _repo = repo;

    public override async Task<ProviderInvoicePagedResultDto?> Handle(
        GetProviderInvoicesQuery request, CancellationToken ct)
    {
        var skip   = (request.Page - 1) * request.PageSize;
        var status = request.Status.HasValue ? (InvoiceStatus?)request.Status.Value : null;
        var type   = request.Type.HasValue   ? (InvoiceType?)request.Type.Value     : null;

        var (items, total) = await _repo.GetProviderInvoicesPagedAsync(
            request.ProviderProfileId, status, type, skip, request.PageSize, request.From, request.To, ct);

        return new ProviderInvoicePagedResultDto
        {
            Items = items.Select(x => new ProviderInvoiceListItemDto
            {
                Id              = x.Id,
                InvoiceNumber   = x.InvoiceNumber,
                InvoiceType     = (int)x.InvoiceType,
                Status          = (int)x.Status,
                SourceType      = (int)x.SourceType,
                SourceId        = x.SourceId,
                Currency        = x.Currency,
                TotalAmount     = x.TotalAmount,
                TaxAmount       = x.TaxAmount,
                RemainingAmount = x.RemainingAmount,
                HasPdf          = x.PdfFileRef != null,
                IssueDateUtc    = x.IssueDateUtc,
                DueDateUtc      = x.DueDateUtc,
                PaidAtUtc       = x.PaidAtUtc,
            }).ToList(),
            Total    = total,
            Page     = request.Page,
            PageSize = request.PageSize,
        };
    }
}
