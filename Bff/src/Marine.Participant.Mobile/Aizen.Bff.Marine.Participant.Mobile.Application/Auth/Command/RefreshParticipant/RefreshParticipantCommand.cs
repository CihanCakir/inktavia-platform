using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Auth;

/// <summary>Returns null on invalid/expired refresh token → the controller yields a clean 401.</summary>
public sealed class RefreshParticipantCommand : AizenCommand<MobileAuthTokenResponse>
{
    public string RefreshToken { get; set; } = default!;
}
