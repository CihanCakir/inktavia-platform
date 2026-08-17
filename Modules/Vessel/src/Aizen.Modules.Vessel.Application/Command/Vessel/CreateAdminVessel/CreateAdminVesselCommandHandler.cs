using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Modules.Vessel.Application.Command.Vessel;

[DocumentationInfo("Create Admin Vessel Command Handler", "Admin vessel creation. Uses the explicitly provided OwnerUserId rather than the caller identity. Assigns the specified user as primary owner and invalidates the vessel list cache.")]
public sealed class CreateAdminVesselCommandHandler : AizenCommandHandler<CreateAdminVesselCommand, CreateVesselResponse>
{
    private readonly IVesselRepository _vesselRepository;
    private readonly IVesselOwnerRepository _ownerRepository;
    private readonly IVesselCacheInvalidationService _invalidation;

    public CreateAdminVesselCommandHandler(
        IVesselRepository vesselRepository,
        IVesselOwnerRepository ownerRepository,
        IVesselCacheInvalidationService invalidation)
    {
        _vesselRepository = vesselRepository;
        _ownerRepository = ownerRepository;
        _invalidation = invalidation;
    }

    public override async Task<CreateVesselResponse?> Handle(CreateAdminVesselCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        var vesselCode = $"V{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        var slug = req.Name.ToLowerInvariant().Replace(" ", "-");

        var entity = VesselEntity.Create(
            vesselCode,
            req.Name,
            slug,
            req.VesselTypeCode,
            req.Description,
            req.VesselUsageTypeCode,
            req.FlagCountryCode,
            req.RegistrationNumber,
            req.MmsiNumber,
            req.ImoNumber,
            req.CallSign,
            req.HomeCountryCode,
            req.HomeCityCode,
            req.HomeDistrictCode,
            req.HomeMarinaName,
            req.Visibility);

        await _vesselRepository.AddAsync(entity, cancellationToken);

        var owner = VesselOwnerEntity.Create(entity.Id, req.OwnerUserId, req.OwnerProfileId, VesselOwnershipRole.PrimaryOwner, true);
        await _ownerRepository.AddAsync(owner, cancellationToken);

        await _invalidation.InvalidateUserVesselListAsync(req.OwnerUserId, cancellationToken);

        return new CreateVesselResponse(entity.ToDto());
    }
}
