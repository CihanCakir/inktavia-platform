using Aizen.Core.Domain;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Domain.Entities.Vessel;

[DocumentationInfo("Vessel owner entity", "Associates a user with a vessel via a role. One primary owner per vessel is enforced.")]
public sealed class VesselOwnerEntity : AizenEntityWithAudit
{
    public long VesselId { get; private set; }
    public long UserId { get; private set; }
    public long? UserProfileId { get; private set; }
    public VesselOwnershipRole Role { get; private set; }
    public VesselOwnershipStatus OwnershipStatus { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTime? InvitedAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public DateTime? RemovedAt { get; private set; }

    public VesselEntity? Vessel { get; private set; }

    public VesselOwnerEntity() { }

    public static VesselOwnerEntity Create(long vesselId, long userId, long? userProfileId, VesselOwnershipRole role, bool isPrimary)
    {
        return new VesselOwnerEntity
        {
            VesselId = vesselId,
            UserId = userId,
            UserProfileId = userProfileId,
            Role = role,
            OwnershipStatus = isPrimary ? VesselOwnershipStatus.Active : VesselOwnershipStatus.Pending,
            IsPrimary = isPrimary,
            InvitedAt = DateTime.UtcNow,
            AcceptedAt = isPrimary ? DateTime.UtcNow : null,
            IsActive = true
        };
    }

    public void ChangeRole(VesselOwnershipRole newRole) => Role = newRole;

    public void Accept()
    {
        OwnershipStatus = VesselOwnershipStatus.Active;
        AcceptedAt = DateTime.UtcNow;
        IsActive = true;
    }

    public void Reject()
    {
        OwnershipStatus = VesselOwnershipStatus.Rejected;
        IsActive = false;
    }

    public void Remove()
    {
        OwnershipStatus = VesselOwnershipStatus.Removed;
        RemovedAt = DateTime.UtcNow;
        IsActive = false;
    }

    public void SetPrimary() => IsPrimary = true;
    public void ClearPrimary() => IsPrimary = false;
}
