using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Request.Media;

[DocumentationInfo("Update vessel media request", "Input model for updating a vessel media item's metadata.")]
public sealed class UpdateVesselMediaRequest
{
    public int SortOrder { get; set; }
    public bool IsCover { get; set; }
}
