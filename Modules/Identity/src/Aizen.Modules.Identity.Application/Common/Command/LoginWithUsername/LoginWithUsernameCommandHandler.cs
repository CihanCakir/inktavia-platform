using Aizen.Core.CQRS.Handler;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.LoginWithUsername;
using Microsoft.AspNetCore.Identity;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Command.LoginWithUsername;

/// <summary>
/// Admin credential login (username + PIN) → Keycloak handoff (Option 2). Identity is the CREDENTIAL authority
/// (verifies the local PIN + lockout); Keycloak is the TOKEN issuer. On success we mint a single-use Keycloak
/// login-ticket for the resolved admin — the SAME ticket service the admin OTP verify handoff uses — and return
/// the handoff shape (<c>redirect_to_keycloak_handoff</c> + <c>loginTicket</c>). The FE exchanges the ticket
/// (<c>loginWithTicket</c>) for real RS256 tokens, so the AdminPanel BFF accepts them and AR2 silent refresh works.
///
/// Failures return <c>verified:false</c> (HTTP 200, anti-enumeration) — mirroring the OTP verify invalid path —
/// so a wrong credential is a clean "invalid credentials" on the FE, NOT the previous HTTP 500 (which came from
/// the S2S remote-call layer throwing on a business-exception envelope).
/// </summary>
public class LoginWithUsernameCommandHandler : AizenCommandHandler<LoginWithUsernameCommand, VerifyProviderOtpLoginResponse>
{
    private readonly UserManager<UserEntity> _userManager;
    private readonly SignInManager<UserEntity> _signInManager;
    private readonly IProviderOtpLoginTicketService _ticketService;

    private const string ClientId = "admin-panel";
    private const string HandoffAction = "redirect_to_keycloak_handoff";
    private const string HandoffRequiredAction = "keycloak_handoff_required";

    public LoginWithUsernameCommandHandler(
        UserManager<UserEntity> userManager,
        SignInManager<UserEntity> signInManager,
        IProviderOtpLoginTicketService ticketService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _ticketService = ticketService;
    }

    public override async Task<VerifyProviderOtpLoginResponse?> Handle(
        LoginWithUsernameCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user is null)
            return InvalidCredentials();

        // Credential verification is unchanged: verify the local PIN, honor lockout.
        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Pin, lockoutOnFailure: true);
        if (!signInResult.Succeeded)
            return InvalidCredentials();

        // Admin gate + Keycloak subject required to vouch for the user to Keycloak (mirrors the OTP admin gate).
        if (string.IsNullOrWhiteSpace(user.KeycloakSubjectId))
            return InvalidCredentials();
        if (!await _userManager.IsInRoleAsync(user, RoleNames.Admin))
            return InvalidCredentials();

        return await MintHandoffAsync(user.KeycloakSubjectId!, cancellationToken);
    }

    private async Task<VerifyProviderOtpLoginResponse> MintHandoffAsync(string keycloakSubjectId, CancellationToken ct)
    {
        try
        {
            var ticket = await _ticketService.MintAsync(keycloakSubjectId, ClientId, ct);
            return new VerifyProviderOtpLoginResponse
            {
                Verified = true,
                NextAction = HandoffAction,
                LoginTicket = ticket.LoginTicket,
                ExpiresInSeconds = ticket.ExpiresInSeconds,
                Message = "Credentials verified. Redirecting to complete sign-in.",
            };
        }
        catch
        {
            // Ticket mint unavailable — credentials WERE valid, but the redirect can't be issued right now.
            return new VerifyProviderOtpLoginResponse
            {
                Verified = true,
                NextAction = HandoffRequiredAction,
                Message = "Credentials verified. Sign-in redirect is temporarily unavailable.",
            };
        }
    }

    private static VerifyProviderOtpLoginResponse InvalidCredentials() => new()
    {
        Verified = false,
        NextAction = HandoffRequiredAction,
        Message = "Invalid username or PIN.",
    };
}
