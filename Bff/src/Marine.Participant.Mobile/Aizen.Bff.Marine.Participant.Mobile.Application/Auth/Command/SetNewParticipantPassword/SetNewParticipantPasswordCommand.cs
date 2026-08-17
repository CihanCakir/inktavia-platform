using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth.Password;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Auth;

public sealed class SetNewParticipantPasswordCommand : AizenCommand<MobileSetNewPasswordResponse>
{
    public string ResetToken { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
    public string ConfirmPassword { get; set; } = default!;
}
