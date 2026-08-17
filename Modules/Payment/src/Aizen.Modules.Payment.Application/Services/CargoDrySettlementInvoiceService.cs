using Aizen.Modules.Payment.Abstraction.Interface;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Application.Commands.CreateCargoDrySettlementStatement;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// Implements ICargoDrySettlementInvoiceService by dispatching
/// CreateCargoDrySettlementStatementCommand via ISender.
///
/// This service bridges the module boundary: CargoDry.Application injects
/// ICargoDrySettlementInvoiceService (from Payment.Abstraction) without needing a
/// compile-time reference to Payment.Application.
///
/// Phase 4C (July 2026).
/// </summary>
[DocumentationInfo("CargoDrySettlementInvoiceService",
    "Cross-module service that allows the CargoDry module to prepare ProviderSettlementStatement " +
    "invoice drafts for monthly sell-through settlements by dispatching an in-process MediatR command " +
    "to the Payment module handler. Does NOT issue the invoice, does NOT create a PaymentTransaction.")]
public sealed class CargoDrySettlementInvoiceService : ICargoDrySettlementInvoiceService
{
    private readonly ISender                                     _sender;
    private readonly ILogger<CargoDrySettlementInvoiceService>  _logger;

    public CargoDrySettlementInvoiceService(
        ISender                                    sender,
        ILogger<CargoDrySettlementInvoiceService>  logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task<CreateCargoDrySettlementStatementResult> PrepareSettlementStatementAsync(
        long              settlementId,
        string            settlementCode,
        long              providerProfileId,
        decimal           providerPayoutAmount,
        decimal           totalSaleAmount,
        decimal           totalCommissionAmount,
        int               totalKitCount,
        string            currencyCode,
        string            productCode,
        DateTime          periodStartUtc,
        DateTime          periodEndUtc,
        long?             payoutRecordId,
        long              preparedByUserId,
        string?           notes,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Preparing settlement statement for CargoDry settlement. " +
            "SettlementId={Id} Code={Code} ProviderPayout={Amount} {Currency}",
            settlementId, settlementCode, providerPayoutAmount, currencyCode);

        var command = new CreateCargoDrySettlementStatementCommand
        {
            SettlementId          = settlementId,
            SettlementCode        = settlementCode,
            ProviderProfileId     = providerProfileId,
            ProviderPayoutAmount  = providerPayoutAmount,
            TotalSaleAmount       = totalSaleAmount,
            TotalCommissionAmount = totalCommissionAmount,
            TotalKitCount         = totalKitCount,
            CurrencyCode          = currencyCode,
            ProductCode           = productCode,
            PeriodStartUtc        = periodStartUtc,
            PeriodEndUtc          = periodEndUtc,
            PayoutRecordId        = payoutRecordId,
            PreparedByUserId      = preparedByUserId,
            Notes                 = notes,
        };

        var result = await _sender.Send(command, ct)
            ?? throw new InvalidOperationException(
                $"CreateCargoDrySettlementStatementCommand returned null for settlement {settlementId}.");

        return result;
    }
}
