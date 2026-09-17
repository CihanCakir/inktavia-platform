using Aizen.Modules.Payment.Abstraction.Interface;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Services;

/// <summary>
/// Split-host implementation of <see cref="ICargoDrySettlementInvoiceService"/>: prepares the ProviderSettlementStatement
/// draft over HTTP (<see cref="ICargoDrySettlementPaymentRemoteCall"/>) instead of the in-process Payment.Application
/// bridge. Registered with TryAdd so the co-hosted in-process service wins; the standalone aizen-cargodry pod uses this.
/// Idempotent on the Payment side (a non-cancelled statement for the settlement returns AlreadyExisted=true).
/// </summary>
[DocumentationInfo("RemoteCargoDrySettlementInvoiceService",
    "HTTP bridge from CargoDry to Payment for settlement statement (invoice) preparation in the split-host topology.")]
public sealed class RemoteCargoDrySettlementInvoiceService : ICargoDrySettlementInvoiceService
{
    private readonly ICargoDrySettlementPaymentRemoteCall             _remote;
    private readonly ILogger<RemoteCargoDrySettlementInvoiceService>  _logger;

    public RemoteCargoDrySettlementInvoiceService(
        ICargoDrySettlementPaymentRemoteCall            remote,
        ILogger<RemoteCargoDrySettlementInvoiceService> logger)
    {
        _remote = remote;
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
            "Preparing settlement statement via Payment remote call. SettlementId={Id} Code={Code}",
            settlementId, settlementCode);

        return await _remote.PrepareSettlementStatementAsync(new PrepareCargoDrySettlementStatementRemoteCallRequest
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
        }, ct) ?? throw new InvalidOperationException(
            $"Payment remote call returned no statement result for settlement {settlementId}.");
    }
}
