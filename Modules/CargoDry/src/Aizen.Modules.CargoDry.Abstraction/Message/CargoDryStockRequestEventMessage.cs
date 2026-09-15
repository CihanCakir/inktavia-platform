using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.CargoDry.Abstraction.Message;

/// <summary>Wave 4A — provider stock-request lifecycle events that drive notifications.</summary>
public enum CargoDryStockRequestEvent
{
    Created  = 1, // provider submitted a request → admin
    Approved = 2, // admin approved (allocated) → provider
    Shipped  = 3, // admin shipped (tracking code) → provider
    Rejected = 4, // admin rejected (reason) → provider
}

/// <summary>
/// Published on provider stock-request transitions. One Notification consumer fans this out to the requesting provider
/// (approved/shipped/rejected) and/or the admin feed (created + a generic admin activity row for the rest).
/// </summary>
public sealed class CargoDryStockRequestEventMessage : AizenBaseMessage
{
    public long                     StockRequestId    { get; set; }
    public string?                  RequestCode       { get; set; }
    public long                     ProviderProfileId { get; set; }
    public string?                  ProductCode       { get; set; }
    public CargoDryStockRequestEvent Event            { get; set; }
    public int?                     RequestedQuantity { get; set; }
    public string?                  TrackingCode      { get; set; }
    public string?                  Reason            { get; set; }
}
