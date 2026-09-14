using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.CargoDry.Application.Commands.RecordCargoDrySupplySale;

/// <summary>
/// CargoDry supply flow — records the retail sale of an activated supply kit against its source CARGODRY_SUPPLY SR.
///
/// Called by the ServiceRequest module (kit-activation correlation) AFTER it has released the platform escrow and
/// completed the SR. This is the ONE and ONLY provider-compensation trigger for a supply sale: it enriches the
/// attribution that kit activation already created (SalePrice=retail + commission per the provider's agreement rate,
/// via the existing ResolveCargoDrySalesAttributionFinancials cascade → SellThroughSettlement roll-up) and records the
/// owner's preferred provider on the first completed supply SR.
///
/// Idempotent, keyed by <see cref="ServiceServiceRequestId"/>: a re-fired activation can never double-credit the provider.
/// </summary>
public sealed class RecordCargoDrySupplySaleCommand : AizenCommand<RecordCargoDrySupplySaleResponse>
{
    /// <summary>The activated kit whose attribution is being enriched.</summary>
    public long   KitId             { get; init; }
    /// <summary>Source CARGODRY_SUPPLY service request id (idempotency key).</summary>
    public long   ServiceRequestId  { get; init; }
    /// <summary>Boat owner user id (for preferred-provider recording).</summary>
    public long   OwnerUserId       { get; init; }
    /// <summary>Retail amount the owner paid into the platform escrow (the sale amount).</summary>
    public decimal SaleAmount        { get; init; }
    /// <summary>ISO 4217 currency of the sale (settlement currency).</summary>
    public string  CurrencyCode      { get; init; } = default!;
    /// <summary>User id to stamp on the financial resolution audit (the acting owner, or system).</summary>
    public long    ResolvedByUserId  { get; init; }
}

public sealed class RecordCargoDrySupplySaleResponse
{
    /// <summary>True when this call enriched the attribution (first time). False = already recorded (idempotent no-op) or no attribution found.</summary>
    public bool  Recorded            { get; init; }
    public long? AttributionId       { get; init; }
    public bool  PreferredProviderSet { get; init; }
    public string? Note              { get; init; }
}
