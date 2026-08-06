using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Contracts.Auth.OtpLogin;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.OtpLogin;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.Auth.Command;



public sealed class ResendOtpLoginBffCommand : AizenCommand<OtpLoginResendResponse>
{
    public string LoginRequestId { get; set; } = default!;
}
