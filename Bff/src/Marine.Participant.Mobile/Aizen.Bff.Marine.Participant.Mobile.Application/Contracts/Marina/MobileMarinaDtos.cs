namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Marina;

/// <summary>A nearby marina for the "pick the nearest marina" picker. Cost-free: id + name + coords + distance only.</summary>
public sealed class MobileNearbyMarinaDto
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    /// <summary>"MARINA" | "FISHING_HARBOR" — lets the picker label/badge fishing shelters.</summary>
    public string Type { get; set; } = default!;
    public string? Province { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DistanceMeters { get; set; }
}
