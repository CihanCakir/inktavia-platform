using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Modules.Vessel.Application.Command.Vessel;

[DocumentationInfo("Create Vessel Command Handler", "Handles vessel creation, assigns primary owner and invalidates vessel list cache.")]
public sealed class CreateVesselCommandHandler : AizenCommandHandler<CreateVesselCommand, CreateVesselResponse>
{
    private readonly IVesselRepository _vesselRepository;
    private readonly IVesselOwnerRepository _ownerRepository;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IAizenInfoAccessor _info;

    public CreateVesselCommandHandler(
        IVesselRepository vesselRepository,
        IVesselOwnerRepository ownerRepository,
        IVesselCacheInvalidationService invalidation,
        IAizenInfoAccessor info)
    {
        _vesselRepository = vesselRepository;
        _ownerRepository = ownerRepository;
        _invalidation = invalidation;
        _info = info;
    }

    public override async Task<CreateVesselResponse?> Handle(CreateVesselCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;

        var vesselCode = $"V{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        var slug = request.Request.Name.ToLowerInvariant().Replace(" ", "-");

        var entity = VesselEntity.Create(
            vesselCode,
            request.Request.Name,
            slug,
            request.Request.VesselTypeCode,
            request.Request.Description,
            request.Request.VesselUsageTypeCode,
            request.Request.FlagCountryCode,
            request.Request.RegistrationNumber,
            request.Request.MmsiNumber,
            request.Request.ImoNumber,
            request.Request.CallSign,
            request.Request.HomeCountryCode,
            request.Request.HomeCityCode,
            request.Request.HomeDistrictCode,
            request.Request.HomeMarinaName,
            request.Request.Visibility);

        // Attach the primary owner through the aggregate so EF assigns the VesselId FK from the vessel's
        // generated key on save (a separate scalar-FK insert captured VesselId before persist → FK violation).
        entity.AddOwner(currentUserId, null, VesselOwnershipRole.PrimaryOwner, true);

        await _vesselRepository.AddAsync(entity, cancellationToken);

        await _invalidation.InvalidateUserVesselListAsync(currentUserId, cancellationToken);

        return new CreateVesselResponse(entity.ToDto());
    }
}
