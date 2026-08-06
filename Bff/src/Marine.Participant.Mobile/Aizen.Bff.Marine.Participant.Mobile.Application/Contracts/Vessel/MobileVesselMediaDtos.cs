namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;

/// <summary>A vessel photo/media item for the mobile gallery (M4f). The URL is a freshly-resolved presigned
/// FileStorage read URL (resolved per-item by the BFF so the list read stays on the page the module invalidates).</summary>
public sealed class MobileVesselMediaDto
{
    public long Id { get; set; }
    public string? MediaType { get; set; }
    public int SortOrder { get; set; }
    public bool IsCover { get; set; }
    public string? Title { get; set; }
    public string? Url { get; set; }
    public DateTime? UrlExpiresAt { get; set; }
}

/// <summary>Attach an already-uploaded (client-side presigned + completed) file to the vessel as a photo.</summary>
public sealed class AttachMobileVesselMediaRequest
{
    public Guid FileId { get; set; }
    /// <summary>Make this the vessel's cover photo (defaults false; the first photo is auto-covered server-side).</summary>
    public bool? IsCover { get; set; }
}

/// <summary>Confirmation of a media deletion.</summary>
public sealed class MobileVesselMediaDeletedDto
{
    public long VesselId { get; set; }
    public long MediaId { get; set; }
}
