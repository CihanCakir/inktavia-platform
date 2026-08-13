namespace Aizen.Modules.Vessel.Abstraction.Dto.Vessel;

/// <summary>
/// Batch id→name projection for a vessel. Used for bulk name enrichment (e.g. the admin service-request list) so the
/// caller resolves many vessel names in one call instead of N+1 detail fetches. Mirrors <see cref="VesselCountByOwnerDto"/>.
/// </summary>
public sealed class VesselNameDto
{
    public long VesselId { get; set; }
    public string Name { get; set; } = default!;
}
