namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

/// <summary>
/// Split-host bridge response for renewal invoice preparation: the created Draft CargoDryInvoice id. Wrapped in a DTO
/// (rather than a bare long) so Refit deserializes a stable JSON object.
/// </summary>
public sealed class PrepareCargoDryRenewalInvoiceRemoteCallResponse
{
    public required long InvoiceId { get; init; }
}
