using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Options;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Auth;

/// <summary>
/// Native social sign-in — Keycloak-unified (validate, don't federate). Validate the provider id_token via
/// Identity → resolve the Keycloak user by verified email (LINK) or create one (email-verified, NO password)
/// → provision the Participant profile + record the external-login link → set attribute + mobile_user role →
/// mint the session the SAME way as OTP/password (login_ticket → ParticipantSessionHandoff). Returns null on
/// an invalid token (controller → 401); no user is created in that case.
/// </summary>
public sealed class SocialLoginParticipantCommandHandler
    : AizenCommandHandler<SocialLoginParticipantCommand, MobileAuthTokenResponse>
{
    private readonly IIdentityRemoteCall _identity;
    private readonly IMarineMobileKeycloakAdminClient _keycloak;
    private readonly IParticipantSessionHandoff _handoff;
    private readonly MarineMobileKeycloakOptions _options;
    private readonly ILogger<SocialLoginParticipantCommandHandler> _logger;

    public SocialLoginParticipantCommandHandler(
        IIdentityRemoteCall identity,
        IMarineMobileKeycloakAdminClient keycloak,
        IParticipantSessionHandoff handoff,
        IOptions<MarineMobileKeycloakOptions> options,
        ILogger<SocialLoginParticipantCommandHandler> logger)
    {
        _identity = identity;
        _keycloak = keycloak;
        _handoff = handoff;
        _options = options.Value;
        _logger = logger;
    }

    public override async Task<MobileAuthTokenResponse?> Handle(
        SocialLoginParticipantCommand request, CancellationToken ct)
    {
        // 1) Validate the native id_token (signature/iss/aud/exp) via Identity. Invalid → 401 (return null).
        Aizen.Modules.Identity.Abstraction.Dto.Participant.ParticipantSocialValidateResult? claims = null;
        try
        {
            var res = await _identity.ValidateParticipantSocial(new MobileSocialValidateRequest
            {
                Provider = request.Provider,
                IdToken = request.IdToken,
                FullName = request.FullName,
            });
            claims = res?.Body;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Social validation failed (provider {Provider}).", request.Provider);
            return null; // invalid/expired/aud-mismatch → 401
        }

        if (claims is null || string.IsNullOrWhiteSpace(claims.Email))
            return null; // no verified email → cannot unify/provision

        var email = claims.Email!.Trim().ToLowerInvariant();
        var firstName = claims.FirstName;
        var lastName = claims.LastName;

        // 2) Resolve the Keycloak user by verified email — LINK if it exists (e.g. registered by password),
        //    else create a new email-verified, password-less user.
        string sub;
        var existing = await _keycloak.FindUserByEmailAsync(email, ct);
        if (existing is not null)
        {
            sub = existing.Id;
        }
        else
        {
            try
            {
                sub = await _keycloak.CreateUserAsync(
                    new CreateKeycloakUserRequest(email, null, firstName, lastName, null), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Keycloak user creation failed during social sign-in.");
                throw new AizenBusinessException("Sign-in could not be completed. Please try again.");
            }
        }

        // 3) Provision the Identity Participant profile + record the external-login link (idempotent).
        long? participantProfileId = null;
        try
        {
            var provision = await _identity.ProvisionParticipantFromKeycloak(new MobileParticipantProvisionRequest
            {
                KeycloakSubjectId = sub,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                EmailVerified = true,
                ExternalProvider = claims.Provider,
                ExternalProviderUserId = claims.ProviderUserId,
            });
            participantProfileId = provision?.Body?.ParticipantProfileId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Identity participant provisioning failed for {Sub}.", sub);
        }

        // 4) participant_profile_id attribute (best-effort).
        if (participantProfileId is > 0)
        {
            try
            {
                await _keycloak.SetUserAttributeAsync(sub, _options.ParticipantProfileIdAttributeName,
                    participantProfileId.Value.ToString(), ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Writing participant_profile_id attribute failed for {Sub}.", sub);
            }
        }

        // 5) mobile_user realm role (idempotent).
        try
        {
            await _keycloak.AssignRealmRoleAsync(sub, _options.MobileUserRole, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Assigning mobile_user role failed for {Sub}.", sub);
            throw new AizenBusinessException("Sign-in could not be completed. Please try again.");
        }

        // 6) Mint the session the uniform way: login_ticket for the sub → handoff → inktavia-mobile tokens.
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
