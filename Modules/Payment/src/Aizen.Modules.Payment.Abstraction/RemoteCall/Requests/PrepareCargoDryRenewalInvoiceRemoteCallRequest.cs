namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;

/// <summary>
/// Split-host bridge request: CargoDry asks Payment to create a Draft CargoDryInvoice for a kit renewal preparation.
/// Mirrors <see cref="Aizen.Modules.Payment.Abstraction.Interface.ICargoDryRenewalInvoiceService.PrepareRenewalInvoiceAsync"/>.
/// The one-invoice-per-preparation guard lives in the CargoDry handler (checks InvoiceId already set), so Payment always
/// creates a new draft. Does NOT create a PaymentTransaction, does NOT call Iyzico.
/// </summary>
public sealed class PrepareCargoDryRenewalInvoiceRemoteCallRequest
{
    public required long    RenewalPreparationId { get; init; }
    public required string  RenewalCode          { get; init; }
    public required long    KitId                { get; init; }
    public required string  KitCode              { get; init; }
    public required string  ProductCode          { get; init; }
    public          string? ProductName          { get; init; }
    public          long?   OwnerUserId          { get; init; }
    public required decimal RenewalPrice         { get; init; }
    public required string  CurrencyCode         { get; init; }
    public required int     RenewalMonths        { get; init; }
    public          string? Note                 { get; init; }
}
