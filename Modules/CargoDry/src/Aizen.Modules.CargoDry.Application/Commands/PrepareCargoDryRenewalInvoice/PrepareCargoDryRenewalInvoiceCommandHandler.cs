using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Application.Commands.PrepareCargoDryKitRenewal;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.Payment.Abstraction.Interface;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Commands.PrepareCargoDryRenewalInvoice;

[DocumentationInfo("PrepareCargoDryRenewalInvoiceCommandHandler",
    "Calls ICargoDryRenewalInvoiceService (Payment.Abstraction bridge) to create a Draft invoice " +
    "for the renewal preparation. Records InvoiceId on the preparation entity. " +
    "Idempotent: if InvoiceId already set, returns existing data without re-creating. " +
    "Does NOT create PaymentTransaction. Does NOT call Iyzico. " +
    "Phase 11 (July 2026).")]
public sealed class PrepareCargoDryRenewalInvoiceCommandHandler
    : AizenCommandHandler<PrepareCargoDryRenewalInvoiceCommand, CargoDryRenewalPreparationDto>
{
    private readonly ICargoDryRenewalPreparationRepository _preparations;
    private readonly ICargoDryProductRepository            _products;
    private readonly ICargoDryRenewalInvoiceService        _invoiceService;
    private readonly ILogger<PrepareCargoDryRenewalInvoiceCommandHandler> _logger;

    public PrepareCargoDryRenewalInvoiceCommandHandler(
        ICargoDryRenewalPreparationRepository              preparations,
        ICargoDryProductRepository                         products,
        ICargoDryRenewalInvoiceService                     invoiceService,
        ILogger<PrepareCargoDryRenewalInvoiceCommandHandler> logger)
    {
        _preparations   = preparations;
        _products       = products;
        _invoiceService = invoiceService;
        _logger         = logger;
    }

    public override async Task<CargoDryRenewalPreparationDto> Handle(
        PrepareCargoDryRenewalInvoiceCommand request, CancellationToken ct)
    {
        var preparation = await _preparations.GetByIdAsync(request.RenewalPreparationId, ct)
            ?? throw new AizenBusinessException(
                $"Renewal preparation {request.RenewalPreparationId} not found.");

        // ── Idempotency ───────────────────────────────────────────────────────
        if (preparation.InvoiceId.HasValue)
        {
            _logger.LogInformation(
                "Renewal preparation {Id} ({Code}) already has InvoiceId={InvoiceId}. Returning existing.",
                preparation.Id, preparation.RenewalCode, preparation.InvoiceId.Value);

            var product = await _products.GetByCodeAsync(preparation.ProductCode, ct);
            return PrepareCargoDryKitRenewalCommandHandler.MapToDto(preparation, product?.Name);
        }

        // ── State guard ───────────────────────────────────────────────────────
        if (preparation.IsTerminal)
            throw new AizenBusinessException(
                $"Renewal preparation {preparation.Id} ({preparation.RenewalCode}) is in terminal status " +
                $"{preparation.Status} — cannot prepare invoice.");

        // ── Enrich product name ───────────────────────────────────────────────
        var productEntity = await _products.GetByCodeAsync(preparation.ProductCode, ct);

        // ── Call Payment module in-process via bridge ─────────────────────────
        var invoiceId = await _invoiceService.PrepareRenewalInvoiceAsync(
            renewalPreparationId: preparation.Id,
            renewalCode:          preparation.RenewalCode,
            kitId:                preparation.KitId,
            kitCode:              preparation.KitCode,
            productCode:          preparation.ProductCode,
            productName:          productEntity?.Name,
            ownerUserId:          preparation.OwnerUserId,
            renewalPrice:         preparation.RenewalPrice,
            currencyCode:         preparation.CurrencyCode,
            renewalMonths:        preparation.RequestedRenewalMonths,
            note:                 request.Note,
            ct:                   ct);

        preparation.MarkInvoicePrepared(invoiceId);
        await _preparations.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Renewal invoice prepared. PreparationId={Id} Code={Code} InvoiceId={InvoiceId}",
            preparation.Id, preparation.RenewalCode, invoiceId);

        return PrepareCargoDryKitRenewalCommandHandler.MapToDto(preparation, productEntity?.Name);
    }
}
