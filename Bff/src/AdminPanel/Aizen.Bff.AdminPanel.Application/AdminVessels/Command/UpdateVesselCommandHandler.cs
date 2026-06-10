using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;
using Aizen.Bff.AdminPanel.Application.Common.Services;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Command;

[DocumentationInfo("UpdateVessel command handler", "Updates a vessel's details via the Vessel module.")]
public sealed class UpdateVesselCommandHandler : AizenCommandHandler<UpdateVesselCommand, UpdateVesselResponse>
{
    private readonly IVesselAdminBffRemoteCall _vessel;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;
    public UpdateVesselCommandHandler(
        IVesselAdminBffRemoteCall vessel,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _vessel = vessel;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<UpdateVesselResponse?> Handle(UpdateVesselCommand request, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
        var authHeader = $"Bearer {serviceToken}";

        var r = await _vessel.UpdateVessel(request.VesselId, request.Request, authHeader, request.UserToken);
        return r.Body;
    }
}
