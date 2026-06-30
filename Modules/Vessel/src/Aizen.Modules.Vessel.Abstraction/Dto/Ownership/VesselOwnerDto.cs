using Aizen.Modules.Vessel.Abstraction.Enum;

namespace Aizen.Modules.Vessel.Abstraction.Dto.Ownership;

[DocumentationInfo("Vessel owner DTO", "Represents a user's ownership relationship with a vessel.")]
public sealed class VesselOwnerDto
{
    public long Id { get; set; }
    public long VesselId { get; set; }
    public long UserId { get; set; }
    public long? UserProfileId { get; set; }
    public VesselOwnershipRole Role { get; set; }
    public VesselOwnershipStatus Status { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime? InvitedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? RemovedAt { get; set; }
}
