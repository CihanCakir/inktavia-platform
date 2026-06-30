using Aizen.Modules.Vessel.Abstraction.Enum;

namespace Aizen.Modules.Vessel.Abstraction.Dto.Status;

[DocumentationInfo("Vessel status history by owner item DTO", "A status change event enriched with vessel name, used for the user activity feed.")]
public sealed class VesselStatusHistoryByOwnerItemDto
{
    public long Id { get; set; }
    public long VesselId { get; set; }
    public string VesselName { get; set; } = default!;
    public VesselStatus? FromStatus { get; set; }
    public VesselStatus ToStatus { get; set; }
    public string? Reason { get; set; }
    public long? ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; }
}
