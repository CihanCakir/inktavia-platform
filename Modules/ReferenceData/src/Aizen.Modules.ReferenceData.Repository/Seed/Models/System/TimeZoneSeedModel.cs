
namespace Aizen.Modules.ReferenceData.Repository.Seed.Models.System;

/// <summary>Seed model for a TimeZone entity, read from time-zones.json.</summary>
[DocumentationInfo("Seed model representing a time zone loaded from JSON.", "Maps to TimeZoneEntity. Idempotency key: Code.")]
public sealed class TimeZoneSeedModel
{
    public string Code { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string UtcOffset { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
