using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Vessel.Application.Mapping;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Modules.Vessel.Application.Command.Vessel;

[DocumentationInfo("Update Vessel Command Handler", "Loads vessel, applies profile updates and invalidates vessel and list caches.")]
public sealed class UpdateVesselCommandHandler : AizenCommandHandler<UpdateVesselCommand, UpdateVesselResponse>
{
    private readonly IVesselRepository _vesselRepository;
    private readonly IVesselCacheInvalidationService _invalidation;
    private readonly IVesselAccessService _accessService;
    private readonly IAizenInfoAccessor _info;

    public UpdateVesselCommandHandler(
        IVesselRepository vesselRepository,
        IVesselCacheInvalidationService invalidation,
        IVesselAccessService accessService,
        IAizenInfoAccessor info)
    {
        _vesselRepository = vesselRepository;
        _invalidation = invalidation;
        _accessService = accessService;
        _info = info;
    }

    public override async Task<UpdateVesselResponse?> Handle(UpdateVesselCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        await _accessService.EnsureCanEditAsync(request.VesselId, currentUserId, cancellationToken);

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
        await _invalidation.InvalidateUserVesselListAsync(currentUserId, cancellationToken);

        return new UpdateVesselResponse(vessel.ToDto());
    }
}
