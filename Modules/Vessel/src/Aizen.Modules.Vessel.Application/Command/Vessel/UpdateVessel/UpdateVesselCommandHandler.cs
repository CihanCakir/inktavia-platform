using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Application.Command.Vessel;

[DocumentationInfo("Update Vessel Command Handler", "Loads vessel, applies profile updates and invalidates vessel and list caches.")]
public sealed class UpdateVesselCommandHandler : AizenCommandHandler<UpdateVesselCommand, VesselDto>
{
    private readonly IVesselRepository _vesselRepository;
    private readonly IVesselCacheInvalidationService _invalidation;

    public UpdateVesselCommandHandler(IVesselRepository vesselRepository, IVesselCacheInvalidationService invalidation)
    {
        _vesselRepository = vesselRepository;
        _invalidation = invalidation;
    }

    public override async Task<VesselDto?> Handle(UpdateVesselCommand request, CancellationToken cancellationToken)
    {
        var vessel = await _vesselRepository.GetByIdAsync(request.VesselId, cancellationToken)
            ?? throw new KeyNotFoundException($"Vessel {request.VesselId} not found.");

        vessel.UpdateProfile(
            request.Request.Name,
            request.Request.Description,
            request.Request.VesselTypeCode,
            request.Request.VesselUsageTypeCode,
            request.Request.FlagCountryCode,
            request.Request.RegistrationNumber,
            request.Request.MmsiNumber,
            request.Request.ImoNumber,
            request.Request.CallSign,
            request.Request.HomeCountryCode,
            request.Request.HomeCityCode,
            request.Request.HomeDistrictCode,
            request.Request.HomeMarinaName);

        _vesselRepository.Update(vessel);

        await _invalidation.InvalidateVesselAsync(vessel.Id, cancellationToken);
        await _invalidation.InvalidateUserVesselListAsync(request.RequestingUserId, cancellationToken);

        return vessel.ToDto();
    }
}
