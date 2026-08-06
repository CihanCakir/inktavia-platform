using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Command;

[DocumentationInfo("Register admin vessel BFF command handler", "Maps the register form payload to CreateAdminVesselRequest and forwards to the Vessel module admin create endpoint.")]
public sealed class RegisterVesselBffCommandHandler
    : AizenCommandHandler<RegisterVesselBffCommand, CreateVesselResponse>
{
    private readonly IVesselRemoteCall _vessel;

    public RegisterVesselBffCommandHandler(
        IVesselRemoteCall vessel)
    {
        _vessel = vessel;
    }

    public override async Task<CreateVesselResponse?> Handle(
        RegisterVesselBffCommand request, CancellationToken cancellationToken)
    {

        var adminRequest = new CreateAdminVesselRequest
        {
            Name = request.Payload.Name,
            Description = request.Payload.Description,
            VesselTypeCode = request.Payload.VesselTypeCode,
            VesselUsageTypeCode = request.Payload.VesselUsageTypeCode,
            FlagCountryCode = request.Payload.FlagCountryCode,
            RegistrationNumber = request.Payload.RegistrationNumber,
            MmsiNumber = request.Payload.MmsiNumber,
            ImoNumber = request.Payload.ImoNumber,
            CallSign = request.Payload.CallSign,
            HomeCountryCode = request.Payload.HomeCountryCode,
            HomeCityCode = request.Payload.HomeCityCode,
            HomeDistrictCode = request.Payload.HomeDistrictCode,
            HomeMarinaName = request.Payload.HomeMarinaName,
            Visibility = request.Payload.Visibility,
            OwnerUserId = request.Payload.OwnerUserId,
            OwnerProfileId = request.Payload.OwnerProfileId
        };

        var result = await _vessel.CreateAdminVessel(adminRequest);
        return result?.Body;
    }
}
