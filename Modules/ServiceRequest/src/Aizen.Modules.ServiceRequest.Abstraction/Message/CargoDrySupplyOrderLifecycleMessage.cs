using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

/// <summary>Wave 4A — CargoDry supply-order lifecycle transitions that carry no other notification-bearing message.</summary>
public enum CargoDrySupplyOrderLifecycleEvent
{
    Assigned         = 1, // a program provider was assigned (owner-facing; provider already notified via AssignmentCreated)
    Delivered        = 2, // provider marked the kit delivered
    AwaitingShipment = 3, // accept window lapsed → cargo/direct fulfilment (owner + admin)
    Shipped          = 4, // admin marked the cargo order shipped (tracking code)
}

/// <summary>
/// Published on the supply-order transitions that otherwise emit no notification-bearing bus message. One Notification
/// consumer fans this out to the owner (always), the assigned provider (where applicable), and the admin feed.
/// Self-contained: carries every field the consumer needs (no SR read-back on the Notification side).
/// </summary>
[DocumentationInfo("CargoDry supply-order lifecycle message", "Drives owner/provider/admin notifications for supply-order transitions.")]
public sealed class CargoDrySupplyOrderLifecycleMessage : AizenBaseMessage
{
    public long                              ServiceRequestId  { get; set; }
    public string?                           RequestCode       { get; set; }
    public long                              OwnerUserId       { get; set; }
    public string?                           ProductCode       { get; set; }
    public CargoDrySupplyOrderLifecycleEvent Event             { get; set; }
    /// <summary>Assigned provider profile id (Assigned/Delivered); null on the cargo path (AwaitingShipment/Shipped).</summary>
    public long?                             ProviderProfileId { get; set; }
    public string?                           TrackingCode      { get; set; }
    public long?                             DeliveredKitId    { get; set; }
}
