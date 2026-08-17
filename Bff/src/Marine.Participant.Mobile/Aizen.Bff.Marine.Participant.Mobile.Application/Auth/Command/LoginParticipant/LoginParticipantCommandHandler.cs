using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Auth;

/// <summary>
/// Password login: verify credentials server-side (ROPC on the confidential marine-mobile-bff, token
/// discarded), then mint the session the SAME uniform way as OTP — login_ticket for the sub → M2c handoff →
/// inktavia-mobile tokens. Returns null on invalid credentials (controller → 401).
/// </summary>
public sealed class LoginParticipantCommandHandler
    : AizenCommandHandler<LoginParticipantCommand, MobileAuthTokenResponse>
{
    private readonly IParticipantKeycloakAuthClient _auth;
    private readonly IIdentityRemoteCall _identity;
    private readonly IParticipantSessionHandoff _handoff;
    private readonly ILogger<LoginParticipantCommandHandler> _logger;

    public LoginParticipantCommandHandler(
        IParticipantKeycloakAuthClient auth,
        IIdentityRemoteCall identity,
        IParticipantSessionHandoff handoff,
        ILogger<LoginParticipantCommandHandler> logger)
    {
        _auth = auth;
        _identity = identity;
        _handoff = handoff;
        _logger = logger;
    }

    public override async Task<MobileAuthTokenResponse?> Handle(
        LoginParticipantCommand request, CancellationToken ct)
    {
        var identifier = (request.Email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(request.Password))
            return null; // → 401

        // The identifier field carries an email OR a phone. Keycloak's username is the email and the phone
        // is NOT stored in Keycloak — so when a phone is supplied, resolve it to the participant's canonical
        // email via Identity (the SAME lookup OTP-login uses) before the ROPC. Same '@'→email/else→phone
        // rule the OTP endpoint uses. An unresolved phone yields invalid credentials (anti-enumeration; not 500).
        var ropcUsername = identifier.ToLowerInvariant();
        if (!identifier.Contains('@'))
        {
            var resolved = await _identity.ResolveParticipantIdentifier(
                new Aizen.Modules.Identity.Abstraction.Dto.OtpLogin.ResolveParticipantIdentifierRequest
                { Channel = "phone", Identifier = identifier });
            var resolvedEmail = resolved?.Body?.Email;
            if (string.IsNullOrWhiteSpace(resolvedEmail))
                return null; // unknown phone → invalid credentials → 401
            ropcUsername = resolvedEmail.Trim().ToLowerInvariant();
        }

        // Verify credentials; token is discarded — we only take the sub.
        var sub = await _auth.VerifyPasswordGetSubAsync(ropcUsername, request.Password, ct);
        if (string.IsNullOrEmpty(sub))
            return null; // invalid credentials → 401

        // Mint the session uniformly (login_ticket → handoff).
        var ticket = await _identity.MintParticipantLoginTicket(
            new Aizen.Modules.Identity.Abstraction.Dto.OtpLogin.MintParticipantTicketRequest { Sub = sub });
        var loginTicket = ticket?.Body?.LoginTicket;
        if (string.IsNullOrEmpty(loginTicket))
            throw new AizenBusinessException("Sign-in could not be completed. Please try again.");

        var tokens = await _handoff.ExchangeAsync(loginTicket, ct);
        return new MobileAuthTokenResponse
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            ExpiresIn = tokens.ExpiresIn,
            TokenType = tokens.TokenType,
        };
    }
}
