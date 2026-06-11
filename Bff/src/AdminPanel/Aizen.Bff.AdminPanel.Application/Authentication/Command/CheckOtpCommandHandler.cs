using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Services;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Authentication.Command;

[DocumentationInfo("CheckOtp command handler", "Validates an OTP code via the Identity module.")]
public sealed class CheckOtpCommandHandler : AizenCommandHandler<CheckOtpCommand, CheckOtpDto>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public CheckOtpCommandHandler(
        IIdentityAdminBffRemoteCall identity,
        IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _identity = identity;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<CheckOtpDto?> Handle(CheckOtpCommand request, CancellationToken ct)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(ct);
        var authHeader = $"Bearer {serviceToken}";

        var r = await _identity.CheckOtp(request.Request, authHeader);
        return r.Body;
    }
}
