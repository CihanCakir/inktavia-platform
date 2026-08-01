using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Auth.OtpLogin;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.Auth.OtpLogin;

// Admin OTP → Keycloak login — a mirror of the MarineProvider BFF OtpLogin handlers. Each handler proxies to the Identity
// module's Keycloak-backed admin OTP-login endpoints (NOT the legacy Identity HS256 login). The Identity module mints the
// Keycloak token (with the "Admin" realm role) and returns a LoginTicket for the FE handoff.

public sealed class RequestOtpLoginCommand : AizenCommand<OtpLoginRequestResponse>
{
    public string Channel    { get; set; } = default!;
    public string Identifier { get; set; } = default!;
}

public sealed class RequestOtpLoginCommandHandler : AizenCommandHandler<RequestOtpLoginCommand, OtpLoginRequestResponse>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly ILogger<RequestOtpLoginCommandHandler> _logger;
    public RequestOtpLoginCommandHandler(IIdentityAdminBffRemoteCall identity, ILogger<RequestOtpLoginCommandHandler> logger)
    { _identity = identity; _logger = logger; }

    public override async Task<OtpLoginRequestResponse?> Handle(RequestOtpLoginCommand request, CancellationToken ct)
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

public sealed class VerifyOtpLoginCommand : AizenCommand<OtpLoginVerifyResponse>
{
    public string LoginRequestId { get; set; } = default!;
    public string OtpCode        { get; set; } = default!;
}

public sealed class VerifyOtpLoginCommandHandler : AizenCommandHandler<VerifyOtpLoginCommand, OtpLoginVerifyResponse>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly ILogger<VerifyOtpLoginCommandHandler> _logger;
    public VerifyOtpLoginCommandHandler(IIdentityAdminBffRemoteCall identity, ILogger<VerifyOtpLoginCommandHandler> logger)
    { _identity = identity; _logger = logger; }

    public override async Task<OtpLoginVerifyResponse?> Handle(VerifyOtpLoginCommand request, CancellationToken ct)
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

public sealed class ResendOtpLoginCommand : AizenCommand<OtpLoginResendResponse>
{
    public string LoginRequestId { get; set; } = default!;
}

public sealed class ResendOtpLoginCommandHandler : AizenCommandHandler<ResendOtpLoginCommand, OtpLoginResendResponse>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly ILogger<ResendOtpLoginCommandHandler> _logger;
    public ResendOtpLoginCommandHandler(IIdentityAdminBffRemoteCall identity, ILogger<ResendOtpLoginCommandHandler> logger)
    { _identity = identity; _logger = logger; }

    public override async Task<OtpLoginResendResponse?> Handle(ResendOtpLoginCommand request, CancellationToken ct)
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
