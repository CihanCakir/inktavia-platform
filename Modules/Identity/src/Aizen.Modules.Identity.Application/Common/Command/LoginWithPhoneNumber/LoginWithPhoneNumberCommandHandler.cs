using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface.Repository;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Microsoft.AspNetCore.Identity;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command
{
    /// <summary>
    /// Admin credential login (phone + password) → Keycloak handoff (Option 2). Same design as
    /// <see cref="LoginWithUsername.LoginWithUsernameCommandHandler"/>: Identity verifies the local password +
    /// lockout, then mints a single-use Keycloak login-ticket (admin-panel client) for the resolved admin and
    /// returns the handoff shape. Failures return <c>verified:false</c> (HTTP 200, anti-enumeration) — never 500.
    /// </summary>
    public class LoginWithPhoneNumberCommandHandler : AizenCommandHandler<LoginWithPhoneNumberCommand, VerifyProviderOtpLoginResponse>
    {
        private readonly UserManager<UserEntity> _userManager;
        private readonly SignInManager<UserEntity> _signInManager;
        private readonly IUserRepository _userRepository;
        private readonly IProviderOtpLoginTicketService _ticketService;

        private const string ClientId = "admin-panel";
        private const string HandoffAction = "redirect_to_keycloak_handoff";
        private const string HandoffRequiredAction = "keycloak_handoff_required";

        public LoginWithPhoneNumberCommandHandler(
            UserManager<UserEntity> userManager,
            SignInManager<UserEntity> signInManager,
            IUserRepository userRepository,
            IProviderOtpLoginTicketService ticketService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userRepository = userRepository;
            _ticketService = ticketService;
        }

        public override async Task<VerifyProviderOtpLoginResponse?> Handle(
            LoginWithPhoneNumberCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByPhoneNumber(request.PhoneNumber, disableTracking: false);
            if (user is null || !user.PhoneNumberConfirmed)
                return InvalidCredentials();

            // Credential verification is unchanged: verify the local password, honor lockout.
            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
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
            Message = "Invalid phone number or password.",
        };
    }
}
