using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Auth;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Auth;

/// <summary>Native Google/Apple sign-in. Returns null on invalid/expired/aud-mismatch token → controller 401.</summary>
public sealed class SocialLoginParticipantCommand : AizenCommand<MobileAuthTokenResponse>
{
    public string Provider { get; set; } = default!;   // "google" | "apple"
    public string IdToken { get; set; } = default!;
    public string? FullName { get; set; }               // Apple first-authorization name
}
