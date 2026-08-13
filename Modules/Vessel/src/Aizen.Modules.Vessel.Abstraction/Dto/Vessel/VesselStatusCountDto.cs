namespace Aizen.Modules.Vessel.Abstraction.Dto.Vessel;

/// <summary>
/// Batch (status → count) projection over the vessel fleet, for the admin dashboard fleet-status chart (C3).
/// Status is the lowercase enum key (e.g. UnderMaintenance → undermaintenance). Mirrors <see cref="VesselNameDto"/>.
/// </summary>
public sealed class VesselStatusCountDto
{
    public string Status { get; set; } = default!;
    public int Count { get; set; }
}
