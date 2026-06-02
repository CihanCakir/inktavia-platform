using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Dto.Media;

[DocumentationInfo("Vessel media DTO", "Photo, video or other media file linked to a vessel.")]
public sealed class VesselMediaDto
{
    public long Id { get; set; }
    public long VesselId { get; set; }
    public VesselMediaType MediaType { get; set; }
    public Guid? FileId { get; set; }
    public string? OriginalFileNameSnapshot { get; set; }
    public string? ContentTypeSnapshot { get; set; }
    public long? SizeInBytesSnapshot { get; set; }
    public int SortOrder { get; set; }
    public bool IsCover { get; set; }
    public bool IsActive { get; set; }
    public string? AccessUrl { get; set; }
    public DateTime? AccessUrlExpiresAt { get; set; }
}

