using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Auth.OtpLogin;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.Auth.Command;



public sealed class VerifyOtpLoginCommandHandler : AizenCommandHandler<VerifyOtpLoginBffCommand, OtpLoginVerifyResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<VerifyOtpLoginCommandHandler> _logger;
    public VerifyOtpLoginCommandHandler(IIdentityRemoteCall identity, ILogger<VerifyOtpLoginCommandHandler> logger)
    { _identity = identity; _logger = logger; }

    public override async Task<OtpLoginVerifyResponse?> Handle(VerifyOtpLoginBffCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _identity.VerifyAdminOtpLogin(new VerifyProviderOtpLoginRequest
            { LoginRequestId = request.LoginRequestId, OtpCode = request.OtpCode });
            var data = result.Body;
            if (data is not null)
                return new OtpLoginVerifyResponse
                {
                    Verified = data.Verified, NextAction = data.NextAction,
                    LoginTicket = data.LoginTicket, ExpiresInSeconds = data.ExpiresInSeconds, Message = data.Message,
                };
        }
        catch (Exception ex) { _logger.LogError(ex, "Identity admin OTP login verify failed."); }
        return new OtpLoginVerifyResponse { Verified = false, NextAction = "keycloak_handoff_required", Message = "Verification failed." };
    }
}
