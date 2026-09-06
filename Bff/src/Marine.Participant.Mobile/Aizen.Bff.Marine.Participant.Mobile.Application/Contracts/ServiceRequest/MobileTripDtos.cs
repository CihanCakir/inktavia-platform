namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;

/// <summary>Owner-facing live-trip snapshot for the map screen's initial render (before the socket attaches).
/// Cost-free: coarse position + timings + an approximate straight-line ETA. Status is the enum name.</summary>
public sealed class MobileServiceRequestTripDto
{
    public long ServiceRequestId { get; set; }
    /// <summary>EnRoute | Arrived | Cancelled.</summary>
    public string Status { get; set; } = default!;
    public DateTime StartedAt { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public double? LastLatitude { get; set; }
    public double? LastLongitude { get; set; }
    public double? LastHeading { get; set; }
    public DateTime? LastPingAt { get; set; }
    /// <summary>Approximate straight-line ETA (minutes); null while not computable / trip terminal. No routing engine.</summary>
    public double? EtaMinutes { get; set; }
    public double? TotalDistanceKm { get; set; }
    public int? DurationSeconds { get; set; }
}
