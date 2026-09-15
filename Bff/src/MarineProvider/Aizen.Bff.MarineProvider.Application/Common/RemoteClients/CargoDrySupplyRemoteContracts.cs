namespace Aizen.Bff.MarineProvider.Application.Common.RemoteClients;

/// <summary>CargoDry supply v2 — provider marks a supply order delivered (body). BFF-local (the module command type
/// lives in the SR Application assembly, which the BFF does not reference); Refit binds structurally by property name.</summary>
public sealed class MarkCargoDryDeliveredRemoteRequest
{
    public long KitId { get; set; }
}

/// <summary>CargoDry supply v2 — SR module reply to mark-delivered. Mirrors the module MarkCargoDrySupplyDeliveredResponse shape.</summary>
public sealed class MarkCargoDryDeliveredRemoteResponse
{
    public long     ServiceRequestId        { get; set; }
    public DateTime DeliveredAtUtc          { get; set; }
    public DateTime AutoCompleteDeadlineUtc { get; set; }
}
