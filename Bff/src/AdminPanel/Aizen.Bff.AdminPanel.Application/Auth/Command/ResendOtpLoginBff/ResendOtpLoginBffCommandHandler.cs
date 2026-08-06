using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Auth.OtpLogin;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.Auth.Command;



public sealed class ResendOtpLoginCommandHandler : AizenCommandHandler<ResendOtpLoginBffCommand, OtpLoginResendResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<ResendOtpLoginCommandHandler> _logger;
    public ResendOtpLoginCommandHandler(IIdentityRemoteCall identity, ILogger<ResendOtpLoginCommandHandler> logger)
    { _identity = identity; _logger = logger; }

    public override async Task<OtpLoginResendResponse?> Handle(ResendOtpLoginBffCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _identity.ResendAdminOtpLogin(new ResendProviderOtpLoginRequest { LoginRequestId = request.LoginRequestId });
            var data = result.Body;
            if (data is not null)
                return new OtpLoginResendResponse
                {
                    Resent = data.Resent, ResendAfterSeconds = data.ResendAfterSeconds,
                    ExpiresInSeconds = data.ExpiresInSeconds, Message = data.Message,
                };
        }
        catch (Exception ex) { _logger.LogError(ex, "Identity admin OTP login resend failed."); }
        return new OtpLoginResendResponse { Resent = false, Message = "Resend failed. Please try again." };
    }
}
