using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Domain.Entities.Invoice;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetInvoiceById;

[DocumentationInfo("Get invoice by ID query handler",
    "Loads a single invoice header; optionally includes line items and tax breakdowns.")]
public sealed class GetInvoiceByIdQueryHandler
    : AizenQueryHandler<GetInvoiceByIdQuery, InvoiceDto>
{
    private readonly IInvoiceRepository _invoices;

    public GetInvoiceByIdQueryHandler(IInvoiceRepository invoices)
        => _invoices = invoices;

    public override async Task<InvoiceDto?> Handle(GetInvoiceByIdQuery request, CancellationToken ct)
    {
        var invoice = request.IncludeLines
            ? await _invoices.GetByIdFullAsync(request.InvoiceId, ct)
            : await _invoices.GetByIdAsync(request.InvoiceId, ct);

        if (invoice is null)
            throw new AizenBusinessException((int)PaymentErrorCode.InvoiceNotFound);

        return MapToDto(invoice, request.IncludeLines);
    }

    internal static InvoiceDto MapToDto(InvoiceHeaderEntity inv, bool includeLines)
    {
        IReadOnlyList<InvoiceLineDto>? lines = includeLines
            ? inv.Lines.Select(l => new InvoiceLineDto(
                l.Id, l.LineNumber, l.LineType, l.Description,
                l.ProductCode, l.ServiceCategoryCode,
                l.Quantity, l.UnitCode, l.UnitPrice,
                l.LineSubTotal, l.DiscountAmount, l.TaxRate, l.TaxAmount, l.LineTotal,
                l.SourceType, l.SourceId)).ToList()
            : null;

        IReadOnlyList<InvoiceTaxBreakdownDto>? breakdowns = includeLines
            ? inv.TaxBreakdowns.Select(b => new InvoiceTaxBreakdownDto(
                b.Id, b.TaxType, b.TaxRate, b.TaxableAmount, b.TaxAmount)).ToList()
            : null;

        return new InvoiceDto(
            inv.Id,
            inv.InvoiceNumber,
            inv.InvoiceType,
            inv.CommercialModel,
            inv.BillingMode,
            inv.Status,
            inv.SourceType,
            inv.SourceId,
            inv.PaymentTransactionId,
            inv.PaymentReleaseId,
            inv.CommissionCalculationId,
            inv.ProviderPayoutId,
            inv.UserSubscriptionId,
            inv.OriginalInvoiceId,
            inv.SellerUserId,
            inv.SellerName,
            inv.SellerTaxNumber,
            inv.SellerTaxOffice,
            inv.SellerAddress,
            inv.BuyerUserId,
            inv.BuyerName,
            inv.BuyerTaxNumber,
            inv.BuyerTaxOffice,
            inv.BuyerAddress,
            inv.Currency,
            inv.SubTotalAmount,
            inv.DiscountAmount,
            inv.TaxableAmount,
            inv.TaxAmount,
            inv.TotalAmount,
            inv.PaidAmount,
            inv.RemainingAmount,
            inv.IssueDateUtc,
            inv.DueDateUtc,
            inv.PaidAtUtc,
            inv.CancelledAtUtc,
            inv.CreateDate,
            inv.ExternalInvoiceId,
            inv.ExternalInvoiceProvider,
            inv.PdfFileRef,
            inv.Notes,
            lines,
            breakdowns);
    }
}
