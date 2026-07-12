using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Contracts.Auth.OtpLogin;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Auth.OtpLogin.RequestOtpLogin;

public sealed class RequestOtpLoginCommandHandler
    : AizenCommandHandler<RequestOtpLoginCommand, OtpLoginRequestResponse>
{
    private readonly IProviderIdentityRemoteCall _identity;
    private readonly ILogger<RequestOtpLoginCommandHandler> _logger;

    public RequestOtpLoginCommandHandler(IProviderIdentityRemoteCall identity, ILogger<RequestOtpLoginCommandHandler> logger)
    { _identity = identity; _logger = logger; }

    public override async Task<OtpLoginRequestResponse?> Handle(RequestOtpLoginCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _identity.RequestProviderOtpLogin(new RequestProviderOtpLoginRequest
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
        catch (Exception ex) { _logger.LogError(ex, "Identity OTP login request failed."); }
        return new OtpLoginRequestResponse { Accepted = true, Message = "If an account exists, a verification code has been sent." };
    }
}
