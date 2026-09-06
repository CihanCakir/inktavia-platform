using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

[DocumentationInfo("Trip DTO", "Owner/provider-facing live trip snapshot. Cost-free: position + timings + coarse ETA only; no economics.")]
public sealed class TripDto
{
    public long ServiceRequestId { get; set; }
    public TripStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public decimal? LastLatitude { get; set; }
    public decimal? LastLongitude { get; set; }
    public decimal? LastHeading { get; set; }
    public DateTime? LastPingAt { get; set; }
    /// <summary>Coarse straight-line ETA (minutes), approximate, null when not computable / trip terminal.</summary>
    public double? EtaMinutes { get; set; }
    public decimal? TotalDistanceKm { get; set; }
    public int? DurationSeconds { get; set; }
}
