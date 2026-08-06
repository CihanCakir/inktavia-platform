using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Auth.OtpLogin;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.Auth.Command;



public sealed class RequestOtpLoginCommandHandler : AizenCommandHandler<RequestOtpLoginBffCommand, OtpLoginRequestResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly ILogger<RequestOtpLoginCommandHandler> _logger;
    public RequestOtpLoginCommandHandler(IIdentityRemoteCall identity, ILogger<RequestOtpLoginCommandHandler> logger)
    { _identity = identity; _logger = logger; }

    public override async Task<OtpLoginRequestResponse?> Handle(RequestOtpLoginBffCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _identity.RequestAdminOtpLogin(new RequestProviderOtpLoginRequest
            { Channel = request.Channel, Identifier = request.Identifier });
            var data = result.Body;
            if (data is not null)
                return new OtpLoginRequestResponse
                {
                    Accepted = data.Accepted, LoginRequestId = data.LoginRequestId,
                    MaskedTarget = data.MaskedTarget, OtpLength = data.OtpLength,
                    ExpiresInSeconds = data.ExpiresInSeconds, ResendAfterSeconds = data.ResendAfterSeconds,
                    Message = data.Message,
                };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Identity admin OTP login request failed.");
            return new OtpLoginRequestResponse { Accepted = false, Message = "Service temporarily unavailable. Please try again later." };
        }

        // Null body → unknown account (anti-enumeration): respond as if accepted.
        return new OtpLoginRequestResponse { Accepted = true, Message = "If an account exists, a verification code has been sent." };
    }
}
