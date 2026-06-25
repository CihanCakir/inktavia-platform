using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Command;

[DocumentationInfo("Register admin vessel BFF command handler", "Maps the register form payload to CreateAdminVesselRequest and forwards to the Vessel module admin create endpoint.")]
public sealed class RegisterAdminVesselBffCommandHandler
    : AizenCommandHandler<RegisterAdminVesselBffCommand, CreateVesselResponse>
{
    private readonly IVesselAdminBffRemoteCall _vessel;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public RegisterAdminVesselBffCommandHandler(
        IVesselAdminBffRemoteCall vessel,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _vessel = vessel;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<CreateVesselResponse?> Handle(
        RegisterAdminVesselBffCommand request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";

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

        var result = await _vessel.CreateAdminVessel(adminRequest, authHeader, request.UserToken);
        return result?.Body;
    }
}
