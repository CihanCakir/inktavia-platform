using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Dto;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetProviderInvoiceById;

public sealed class GetProviderInvoiceByIdQueryHandler
    : AizenQueryHandler<GetProviderInvoiceByIdQuery, ProviderInvoiceDetailDto>
{
    private static readonly InvoiceType[] ProviderTypes =
    {
        InvoiceType.CommissionInvoice,
        InvoiceType.SubscriptionInvoice,
        InvoiceType.ProviderSettlementStatement,
    };

    private readonly IInvoiceRepository _repo;

    public GetProviderInvoiceByIdQueryHandler(IInvoiceRepository repo) => _repo = repo;

    public override async Task<ProviderInvoiceDetailDto?> Handle(
        GetProviderInvoiceByIdQuery request, CancellationToken ct)
    {
        var invoice = await _repo.GetByIdFullAsync(request.InvoiceId, ct);

        if (invoice is null
            || invoice.BuyerUserId != request.ProviderProfileId
            || !ProviderTypes.Contains(invoice.InvoiceType))
            return null;

        return new ProviderInvoiceDetailDto
        {
            Id              = invoice.Id,
            InvoiceNumber   = invoice.InvoiceNumber,
            InvoiceType     = (int)invoice.InvoiceType,
            Status          = (int)invoice.Status,
            SourceType      = (int)invoice.SourceType,
            SourceId        = invoice.SourceId,
            Currency        = invoice.Currency,
            TotalAmount     = invoice.TotalAmount,
            TaxAmount       = invoice.TaxAmount,
            RemainingAmount = invoice.RemainingAmount,
            HasPdf          = invoice.PdfFileRef != null,
            IssueDateUtc    = invoice.IssueDateUtc,
            DueDateUtc      = invoice.DueDateUtc,
            PaidAtUtc       = invoice.PaidAtUtc,
            SellerName      = invoice.SellerName,
            BuyerName       = invoice.BuyerName,
            SubTotalAmount  = invoice.SubTotalAmount,
            DiscountAmount  = invoice.DiscountAmount,
            TaxableAmount   = invoice.TaxableAmount,
            PaidAmount      = invoice.PaidAmount,
            Notes           = invoice.Notes,
            Lines = invoice.Lines.Select(l => new ProviderInvoiceLineDto
            {
                Description = l.Description,
                Quantity    = l.Quantity,
                UnitPrice   = l.UnitPrice,
                LineTotal   = l.LineTotal,
                TaxRate     = l.TaxRate,
                TaxAmount   = l.TaxAmount,
            }).ToList(),
        };
    }
}
