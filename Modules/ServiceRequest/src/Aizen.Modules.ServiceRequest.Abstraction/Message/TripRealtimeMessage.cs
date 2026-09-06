using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Message;

/// <summary>
/// Phase-2 — a live trip event fanned out to the owner map. Published by the SR module on start / location / arrive /
/// cancel; the marine-mobile BFF consumes it and broadcasts to the SignalR group <c>trip:{ServiceRequestId}</c> (the
/// owner joins that group only after an ownership check). Carries only coarse position + a straight-line ETA — no
/// economics. <c>OwnerUserId</c> is carried for parity/debug but the socket group is keyed by <c>ServiceRequestId</c>.
/// </summary>
[DocumentationInfo("Trip realtime message", "Live provider-trip event for the owner map fan-out (trip:{serviceRequestId}).")]
public sealed class TripRealtimeMessage : AizenBaseMessage
{
    public long ServiceRequestId { get; set; }
    public long OwnerUserId { get; set; }
    public long ProviderProfileId { get; set; }
    public TripEventType EventType { get; set; }
    public TripStatus Status { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? Heading { get; set; }
    public DateTime? PingAt { get; set; }
    /// <summary>Coarse straight-line ETA (minutes), null-safe, approximate — no routing engine.</summary>
    public double? EtaMinutes { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
