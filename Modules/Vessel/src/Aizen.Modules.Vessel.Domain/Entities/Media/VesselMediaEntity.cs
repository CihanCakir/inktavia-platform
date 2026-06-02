using Aizen.Core.Domain;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Domain.Entities.Vessel;

[DocumentationInfo("Vessel media entity", "Photo, video or other media file linked to a vessel.")]
public sealed class VesselMediaEntity : AizenEntityWithAudit
{
    public long VesselId { get; private set; }
    public VesselMediaType MediaType { get; private set; }
    public string? FileId { get; private set; }
    public string? FileName { get; private set; }
    public string? FileUrl { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsCover { get; private set; }

    public VesselEntity? Vessel { get; private set; }

    public VesselMediaEntity() { }

    public static VesselMediaEntity Create(
        long vesselId, VesselMediaType mediaType,
        string? fileId, string? fileName, string? fileUrl,
        int sortOrder, bool isCover)
    {
        return new VesselMediaEntity
        {
            VesselId = vesselId,
            MediaType = mediaType,
            FileId = fileId,
            FileName = fileName,
            FileUrl = fileUrl,
            SortOrder = sortOrder,
            IsCover = isCover,
            IsActive = true
        };
    }

    public void Update(int sortOrder, bool isCover)
    {
        SortOrder = sortOrder;
        IsCover = isCover;
    }

    public void SetCover() => IsCover = true;
    public void ClearCover() => IsCover = false;
    public void ChangeSortOrder(int sortOrder) => SortOrder = sortOrder;
    public void Deactivate() => IsActive = false;
}
