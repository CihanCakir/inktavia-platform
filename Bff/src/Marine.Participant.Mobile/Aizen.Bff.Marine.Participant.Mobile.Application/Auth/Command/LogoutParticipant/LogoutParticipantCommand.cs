using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Auth;

public sealed class LogoutParticipantCommand : AizenCommand<MobileLogoutResponse>
{
    public string RefreshToken { get; set; } = default!;
}
