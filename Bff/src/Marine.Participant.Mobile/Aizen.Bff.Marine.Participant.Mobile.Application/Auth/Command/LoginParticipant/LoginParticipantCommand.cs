using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Auth;

/// <summary>Returns null on invalid credentials → the controller yields a clean 401.</summary>
public sealed class LoginParticipantCommand : AizenCommand<MobileAuthTokenResponse>
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
}
