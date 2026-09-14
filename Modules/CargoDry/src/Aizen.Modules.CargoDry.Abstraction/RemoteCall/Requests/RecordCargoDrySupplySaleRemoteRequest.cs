namespace Aizen.Modules.CargoDry.Abstraction.RemoteCall.Requests;

/// <summary>
/// ServiceRequest → CargoDry: record the retail sale of an activated supply kit against its source CARGODRY_SUPPLY SR.
/// Enriches the kit-activation attribution (SalePrice + agreement commission) + records the owner's preferred provider.
/// Idempotent, keyed by <see cref="ServiceRequestId"/>.
/// </summary>
public sealed class RecordCargoDrySupplySaleRemoteRequest
{
    public long    KitId            { get; init; }
    public long    ServiceRequestId { get; init; }
    public long    OwnerUserId      { get; init; }
    public decimal SaleAmount       { get; init; }
    public string  CurrencyCode     { get; init; } = default!;
    public long    ResolvedByUserId { get; init; }
}
