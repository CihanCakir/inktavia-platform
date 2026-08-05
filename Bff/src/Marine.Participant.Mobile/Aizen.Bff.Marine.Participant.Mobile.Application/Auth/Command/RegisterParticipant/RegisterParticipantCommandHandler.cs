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
/// Mirrors <c>RegisterProviderCommandHandler</c>: create the Keycloak user, provision/link the Identity
/// Participant profile, set the profile-id attribute + mobile_user role, send the (non-blocking) verify-email,
/// then mint a session via the SAME M2c handoff (login_ticket → auth-code+PKCE → inktavia-mobile token).
/// </summary>
public sealed class RegisterParticipantCommandHandler
    : AizenCommandHandler<RegisterParticipantCommand, MobileAuthTokenResponse>
{
    private readonly IMarineMobileKeycloakAdminClient _keycloak;
    private readonly IIdentityRemoteCall _identity;
    private readonly IParticipantSessionHandoff _handoff;
    private readonly MarineMobileKeycloakOptions _options;
    private readonly ILogger<RegisterParticipantCommandHandler> _logger;

    public RegisterParticipantCommandHandler(
        IMarineMobileKeycloakAdminClient keycloak,
        IIdentityRemoteCall identity,
        IParticipantSessionHandoff handoff,
        IOptions<MarineMobileKeycloakOptions> options,
        ILogger<RegisterParticipantCommandHandler> logger)
    {
        _keycloak = keycloak;
        _identity = identity;
        _handoff = handoff;
        _options = options.Value;
        _logger = logger;
    }

    public override async Task<MobileAuthTokenResponse?> Handle(
        RegisterParticipantCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new AizenBusinessException("Email and password are required.");

        var email = request.Email.Trim().ToLowerInvariant();
        var (firstName, lastName) = SplitFullName(request.FullName);

        // Duplicate registration → clean business error (no partial user created).
        var existing = await _keycloak.FindUserByEmailAsync(email, ct);
        if (existing is not null)
            throw new AizenBusinessException("An account already exists for this email. Please sign in instead.");

        // 1) Create the Keycloak user (sub == Keycloak user id).
        string sub;
        try
        {
            sub = await _keycloak.CreateUserAsync(
                new CreateKeycloakUserRequest(email, request.Password, firstName, lastName, null), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keycloak user creation failed during participant registration.");
            throw new AizenBusinessException("Account could not be created. Please try again.");
        }

        // 2) Provision/link the Identity Participant profile (idempotent).
        long? participantProfileId = null;
        try
        {
            var provision = await _identity.ProvisionParticipantFromKeycloak(new MobileParticipantProvisionRequest
            {
                KeycloakSubjectId = sub,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                ContactPhone = request.Phone,
                EmailVerified = false
            });
            participantProfileId = provision?.Body?.ParticipantProfileId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Identity participant provisioning failed for {Sub}.", sub);
        }

        // 3) Write participant_profile_id attribute (best-effort).
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

        // 4) Assign the mobile_user realm role (required for /me + module access).
        try
        {
            await _keycloak.AssignRealmRoleAsync(sub, _options.MobileUserRole, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Assigning mobile_user role failed for {Sub}.", sub);
            throw new AizenBusinessException("Account setup could not be completed. Please try again.");
        }

        // 5) Send the verify-email as a NON-BLOCKING courtesy. execute-actions-email adds a VERIFY_EMAIL
        //    required action which Keycloak would otherwise enforce at the immediate session mint AND at every
        //    future login — so we clear the user's required actions right after (email still delivered; login
        //    is not gated, per the locked decision + realm verifyEmail=false).
        try
        {
            await _keycloak.SendVerifyEmailAsync(sub, ct);
            await _keycloak.ClearRequiredActionsAsync(sub, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sending verify-email failed for {Sub} (non-blocking).", sub);
            // Ensure the session mint is never gated even if the email step half-failed.
            try { await _keycloak.ClearRequiredActionsAsync(sub, ct); } catch { /* best-effort */ }
        }

        // 6) Mint the session the uniform way: login_ticket for the new sub → handoff → inktavia-mobile tokens.
        var ticket = await _identity.MintParticipantLoginTicket(
            new Aizen.Modules.Identity.Abstraction.Dto.OtpLogin.MintParticipantTicketRequest { Sub = sub });
        var loginTicket = ticket?.Body?.LoginTicket;
        if (string.IsNullOrEmpty(loginTicket))
            throw new AizenBusinessException("Account created but sign-in could not be completed. Please sign in.");

        var tokens = await _handoff.ExchangeAsync(loginTicket, ct);
        return new MobileAuthTokenResponse
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            ExpiresIn = tokens.ExpiresIn,
            TokenType = tokens.TokenType,
        };
    }

    private static (string firstName, string lastName) SplitFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return (string.Empty, string.Empty);
        var parts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1 ? (parts[0], string.Empty) : (parts[0], parts[1]);
    }
}
