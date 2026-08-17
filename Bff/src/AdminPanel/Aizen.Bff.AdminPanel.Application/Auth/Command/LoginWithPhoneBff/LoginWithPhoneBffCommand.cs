using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Aizen.Modules.Identity.Abstraction.Request;

namespace Aizen.Bff.AdminPanel.Application.Auth.Command;

// Credential login → Keycloak handoff (Option 2): returns the OTP-verify-shaped handoff response.
public sealed class LoginWithPhoneBffCommand : AizenCommand<VerifyProviderOtpLoginResponse>
{
    public LoginWithPhoneRequest Request { get; }
    public LoginWithPhoneBffCommand(LoginWithPhoneRequest request) { Request = request; }
}
