using Aizen.Modules.Vessel.Abstraction.Enum;

namespace Aizen.Modules.Vessel.Abstraction.Dto.Status;

[DocumentationInfo("Vessel status history DTO", "Records a status transition event on a vessel.")]
public sealed class VesselStatusHistoryDto
{
    public long Id { get; set; }
    public long VesselId { get; set; }
    public VesselStatus? FromStatus { get; set; }
    public VesselStatus ToStatus { get; set; }
    public string? Reason { get; set; }
    public long? ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; }
}
