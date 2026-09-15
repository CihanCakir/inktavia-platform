using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.CargoDry.Abstraction.Message;

/// <summary>
/// CargoDry supply v2 — published by the ServiceRequest module when a CARGODRY_SUPPLY order completes via a JOB
/// (delivered auto-complete, or cargo shipped auto-complete / admin manual). A CargoDry consumer records the retail
/// revenue in-process (no token): provider path → SalesAttribution + commission; cargo path → CargoDryDirectSale.
/// Idempotent per <see cref="ServiceRequestId"/>. (The synchronous owner-QR-activation path is unchanged — it records
/// the sale via the BFF-orchestrated CompleteCargoDrySupplyOnActivation flow, not this message.)
/// </summary>
public sealed class CargoDrySupplyOrderCompletedMessage : AizenBaseMessage
{
    public long           ServiceRequestId  { get; set; }
    public string         ProductCode       { get; set; } = default!;
    public decimal        SaleAmount        { get; set; }
    public string         CurrencyCode      { get; set; } = default!;
    public long           OwnerUserId       { get; set; }
    /// <summary>Provider path only (delivered kit). Null for a cargo (direct online) sale.</summary>
    public long?          ProviderProfileId { get; set; }
    /// <summary>Provider path only — the delivered kit whose attribution/commission accrues.</summary>
    public long?          DeliveredKitId    { get; set; }
    /// <summary>True = cargo (direct online sale, no provider) → CargoDryDirectSale; false = provider path → attribution.</summary>
    public bool           IsCargoSale       { get; set; }
    public string?        TrackingCode      { get; set; }
    public DateTimeOffset? ShippedAtUtc     { get; set; }
    /// <summary>System actor id stamped on the financial resolution audit.</summary>
    public long           ResolvedByUserId  { get; set; }
}
