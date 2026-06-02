using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Dto.Media;

[DocumentationInfo("Vessel media DTO", "Photo, video or other media file linked to a vessel.")]
public sealed class VesselMediaDto
{
    public long Id { get; set; }
    public long VesselId { get; set; }
    public VesselMediaType MediaType { get; set; }
    public string? FileId { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsCover { get; set; }
    public bool IsActive { get; set; }
}
