using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.Invoice;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Repository.Seed;

public sealed class InvoiceMockSeed
{
    private readonly PaymentDbContext _db;
    private readonly ILogger<InvoiceMockSeed> _logger;

    public InvoiceMockSeed(PaymentDbContext db, ILogger<InvoiceMockSeed> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        const long provider2 = 100011;

        if (await _db.InvoiceHeaders.AnyAsync(
                x => x.BuyerUserId == provider2
                  && x.InvoiceType == InvoiceType.ProviderSettlementStatement, ct))
        {
            _logger.LogDebug("Provider2 invoice seed skipped — data already present.");
            return;
        }

        // Settlement statement — Issued
        var inv1 = InvoiceHeaderEntity.CreateDraft(
            invoiceType:     InvoiceType.ProviderSettlementStatement,
            commercialModel: CommercialModel.ConsignmentSettlement,
            sourceType:      InvoiceSourceType.CargoDrySettlement,
            sourceId:        1,
            sellerName:      "Inktavia Marine OS",
            buyerName:       "Provider 2 AS",
            currency:        "TRY",
            subTotalAmount:  60m,
            discountAmount:  0m,
            taxableAmount:   60m,
            taxAmount:       10.80m,
            totalAmount:     70.80m,
            buyerUserId:     provider2,
            notes:           "Dev seed — CargoDry settlement statement for provider2");

        await _db.InvoiceHeaders.AddAsync(inv1, ct);
        await _db.SaveChangesAsync(ct);

        var line1 = InvoiceLineEntity.Create(
            invoiceHeaderId: inv1.Id,
            lineNumber:      1,
            lineType:        InvoiceLineType.ProviderSettlementLine,
            description:     "CargoDry consignment payout — July 2026",
            quantity:        1m,
            unitPrice:       60m,
            taxRate:         0.18m,
            sourceType:      InvoiceSourceType.CargoDrySettlement,
            sourceId:        1);

        inv1.AddLine(line1);
        inv1.Issue("PST-DEV-2026-0001", null);

        _db.InvoiceHeaders.Update(inv1);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Provider2 invoice seed complete: 1 ProviderSettlementStatement.");
    }
}
