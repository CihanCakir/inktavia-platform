namespace Aizen.Modules.Payment.Abstraction.Interface;

/// <summary>
/// Bridges CargoDry.Application to Payment.Application for renewal invoice preparation
/// without creating a direct project dependency on Payment.Application.
/// Implemented in Payment.Application and registered via DI when the Payment module starts.
/// Phase 11 (July 2026).
/// </summary>
public interface ICargoDryRenewalInvoiceService
{
    /// <summary>
    /// Creates a Draft CargoDryInvoice for a kit renewal preparation.
    /// Does NOT create a PaymentTransaction. Does NOT call Iyzico.
    /// Idempotent: returns existing invoice id if already prepared for this renewal preparation.
    /// </summary>
    Task<long> PrepareRenewalInvoiceAsync(
        long    renewalPreparationId,
        string  renewalCode,
        long    kitId,
        string  kitCode,
        string  productCode,
        string? productName,
        long?   ownerUserId,
        decimal renewalPrice,
        string  currencyCode,
        int     renewalMonths,
        string? note,
        CancellationToken ct = default);
}
