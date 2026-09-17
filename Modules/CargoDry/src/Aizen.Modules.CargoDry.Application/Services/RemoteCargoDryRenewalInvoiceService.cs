using Aizen.Modules.Payment.Abstraction.Interface;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Services;

/// <summary>
/// Split-host implementation of <see cref="ICargoDryRenewalInvoiceService"/>: creates the Draft renewal CargoDryInvoice
/// over HTTP (<see cref="ICargoDrySettlementPaymentRemoteCall"/>) instead of the in-process Payment.Application bridge.
/// Registered with TryAdd so the co-hosted in-process service wins; the standalone aizen-cargodry pod uses this. The
/// one-invoice-per-preparation guard stays in the CargoDry handler, so Payment always creates a new draft.
/// </summary>
[DocumentationInfo("RemoteCargoDryRenewalInvoiceService",
    "HTTP bridge from CargoDry to Payment for kit renewal invoice preparation in the split-host topology.")]
public sealed class RemoteCargoDryRenewalInvoiceService : ICargoDryRenewalInvoiceService
{
    private readonly ICargoDrySettlementPaymentRemoteCall          _remote;
    private readonly ILogger<RemoteCargoDryRenewalInvoiceService>  _logger;

    public RemoteCargoDryRenewalInvoiceService(
        ICargoDrySettlementPaymentRemoteCall         remote,
        ILogger<RemoteCargoDryRenewalInvoiceService> logger)
    {
        _remote = remote;
        _logger = logger;
    }

    public async Task<long> PrepareRenewalInvoiceAsync(
        long              renewalPreparationId,
        string            renewalCode,
        long              kitId,
        string            kitCode,
        string            productCode,
        string?           productName,
        long?             ownerUserId,
        decimal           renewalPrice,
        string            currencyCode,
        int               renewalMonths,
        string?           note,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Preparing renewal invoice via Payment remote call. RenewalPreparationId={Id} RenewalCode={Code} KitCode={Kit}",
            renewalPreparationId, renewalCode, kitCode);

        var response = await _remote.PrepareRenewalInvoiceAsync(new PrepareCargoDryRenewalInvoiceRemoteCallRequest
        {
            RenewalPreparationId = renewalPreparationId,
            RenewalCode          = renewalCode,
            KitId                = kitId,
            KitCode              = kitCode,
            ProductCode          = productCode,
            ProductName          = productName,
            OwnerUserId          = ownerUserId,
            RenewalPrice         = renewalPrice,
            CurrencyCode         = currencyCode,
            RenewalMonths        = renewalMonths,
            Note                 = note,
        }, ct) ?? throw new InvalidOperationException(
            $"Payment remote call returned no renewal invoice result for preparation {renewalPreparationId}.");

        return response.InvoiceId;
    }
}
