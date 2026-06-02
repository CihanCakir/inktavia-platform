using Aizen.Core.Domain;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Domain.Entities.Vessel;

[DocumentationInfo("Vessel media entity", "Photo, video or other media file linked to a vessel.")]
public sealed class VesselMediaEntity : AizenEntityWithAudit
{
    public long VesselId { get; private set; }
    public VesselMediaType MediaType { get; private set; }
    public Guid? FileId { get; private set; }
    public string? OriginalFileNameSnapshot { get; private set; }
    public string? ContentTypeSnapshot { get; private set; }
    public long? SizeInBytesSnapshot { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsCover { get; private set; }

    public VesselEntity? Vessel { get; private set; }

    public VesselMediaEntity() { }

    public static VesselMediaEntity Create(
        long vesselId,
        VesselMediaType mediaType,
        Guid? fileId,
        string? originalFileNameSnapshot,
        string? contentTypeSnapshot,
        long? sizeInBytesSnapshot,
        int sortOrder,
        bool isCover)
    {
        return new VesselMediaEntity
        {
            VesselId = vesselId,
            MediaType = mediaType,
            FileId = fileId,
            OriginalFileNameSnapshot = originalFileNameSnapshot,
            ContentTypeSnapshot = contentTypeSnapshot,
            SizeInBytesSnapshot = sizeInBytesSnapshot,
            SortOrder = sortOrder,
            IsCover = isCover,
            IsActive = true
        };
    }

    public void Update(
        VesselMediaType mediaType,
        Guid? fileId,
        string? originalFileNameSnapshot,
        string? contentTypeSnapshot,
        long? sizeInBytesSnapshot,
        int sortOrder,
        bool isCover)
    {
        MediaType = mediaType;
        FileId = fileId;
        OriginalFileNameSnapshot = originalFileNameSnapshot;
        ContentTypeSnapshot = contentTypeSnapshot;
        SizeInBytesSnapshot = sizeInBytesSnapshot;
        SortOrder = sortOrder;
        IsCover = isCover;
    }

    public void SetCover() => IsCover = true;
    public void ClearCover() => IsCover = false;
    public void ChangeSortOrder(int sortOrder) => SortOrder = sortOrder;
    public void Deactivate() => IsActive = false;
}

