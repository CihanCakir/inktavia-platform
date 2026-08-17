using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Auth.OtpLogin;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.Auth.Command;



// Admin OTP → Keycloak login — a mirror of the MarineProvider BFF OtpLogin handlers. Each handler proxies to the Identity
// module's Keycloak-backed admin OTP-login endpoints (NOT the legacy Identity HS256 login). The Identity module mints the
// Keycloak token (with the "Admin" realm role) and returns a LoginTicket for the FE handoff.

public sealed class RequestOtpLoginBffCommand : AizenCommand<OtpLoginRequestResponse>
{
    public string Channel    { get; set; } = default!;
    public string Identifier { get; set; } = default!;
}
