using Aizen.Modules.Vessel.Abstraction.Enum;

namespace Aizen.Modules.Vessel.Abstraction.Request.Media;

[DocumentationInfo("Add vessel media request", "Input model for adding a photo, video or file to a vessel.")]
public sealed class AddVesselMediaRequest
{
    public VesselMediaType MediaType { get; set; }
    public Guid? FileId { get; set; }
    public int SortOrder { get; set; }
    public bool IsCover { get; set; }
}

