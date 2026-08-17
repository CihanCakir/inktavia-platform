using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth.OtpLogin;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Auth;

public sealed class ResendParticipantOtpLoginCommand : AizenCommand<MobileOtpResendResponse>
{
    public string LoginRequestId { get; set; } = default!;
}
