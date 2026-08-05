using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth.Password;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Auth;

public sealed class VerifyParticipantPasswordOtpCommand : AizenCommand<MobileVerifyPasswordOtpResponse>
{
    public string ResetRequestId { get; set; } = default!;
    public string OtpCode { get; set; } = default!;
}
