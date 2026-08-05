using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth.OtpLogin;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Auth;

public sealed class RequestParticipantOtpLoginCommand : AizenCommand<MobileOtpSendResponse>
{
    public string Channel { get; set; } = default!;
    public string Identifier { get; set; } = default!;
}
