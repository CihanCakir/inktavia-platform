using Aizen.Modules.Vessel.Abstraction.Dto.Ownership;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Request.Ownership;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Repository.Service;

[DocumentationInfo("Vessel ownership service", "Manages vessel owner relationships, roles and primary owner protection.")]
public sealed class VesselOwnershipService : IVesselOwnershipService
{
    private readonly IVesselOwnerRepository _ownerRepository;

    public VesselOwnershipService(IVesselOwnerRepository ownerRepository)
    {
        _ownerRepository = ownerRepository;
    }

    public async Task<VesselOwnerDto> AddOwnerAsync(long vesselId, AddVesselOwnerRequest request, CancellationToken ct = default)
    {
        var existing = await _ownerRepository.GetByVesselAndUserAsync(vesselId, request.UserId, ct);
        if (existing is not null)
            throw new InvalidOperationException("User is already an owner of this vessel.");

        var entity = VesselOwnerEntity.Create(vesselId, request.UserId, null, request.Role, false);
        await _ownerRepository.AddAsync(entity, ct);
        return MapToDto(entity);
    }

    public async Task<VesselOwnerDto> UpdateOwnerRoleAsync(long vesselId, long ownerId, VesselOwnershipRole role, CancellationToken ct = default)
    {
        var entity = await _ownerRepository.GetByIdAsync(ownerId, ct)
            ?? throw new KeyNotFoundException($"Owner {ownerId} not found.");
        entity.ChangeRole(role);
        _ownerRepository.Update(entity);
        return MapToDto(entity);
    }

    public async Task RemoveOwnerAsync(long vesselId, long ownerId, CancellationToken ct = default)
    {
        var entity = await _ownerRepository.GetByIdAsync(ownerId, ct)
            ?? throw new KeyNotFoundException($"Owner {ownerId} not found.");
        if (entity.IsPrimary)
            throw new InvalidOperationException("Cannot remove the primary owner.");
        entity.Remove();
        _ownerRepository.Update(entity);
    }

    public async Task AcceptInvitationAsync(long vesselId, long userId, CancellationToken ct = default)
    {
        var entity = await _ownerRepository.GetByVesselAndUserAsync(vesselId, userId, ct)
            ?? throw new KeyNotFoundException("Invitation not found.");
        entity.Accept();
        _ownerRepository.Update(entity);
    }

    public async Task RejectInvitationAsync(long vesselId, long userId, CancellationToken ct = default)
    {
        var entity = await _ownerRepository.GetByVesselAndUserAsync(vesselId, userId, ct)
            ?? throw new KeyNotFoundException("Invitation not found.");
        entity.Reject();
        _ownerRepository.Update(entity);
    }

    public async Task SetPrimaryOwnerAsync(long vesselId, long ownerId, CancellationToken ct = default)
    {
        var owners = await _ownerRepository.GetByVesselIdAsync(vesselId, ct);
        foreach (var o in owners.Where(o => o.IsPrimary && o.Id != ownerId))
        {
            o.ClearPrimary();
            _ownerRepository.Update(o);
        }
        var target = owners.FirstOrDefault(o => o.Id == ownerId)
            ?? throw new KeyNotFoundException($"Owner {ownerId} not found.");
        target.SetPrimary();
        _ownerRepository.Update(target);
    }

    public async Task<IReadOnlyList<VesselOwnerDto>> GetOwnersAsync(long vesselId, CancellationToken ct = default)
    {
        var owners = await _ownerRepository.GetByVesselIdAsync(vesselId, ct);
        return owners.Select(MapToDto).ToList();
    }

    private static VesselOwnerDto MapToDto(VesselOwnerEntity e) => new()
    {
        Id = e.Id,
        VesselId = e.VesselId,
        UserId = e.UserId,
        Role = e.Role,
        Status = e.OwnershipStatus,
        IsPrimary = e.IsPrimary,
        InvitedAt = e.InvitedAt,
        AcceptedAt = e.AcceptedAt,
    };
}
