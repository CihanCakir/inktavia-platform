namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Profile;

/// <summary>Body of GET/PUT /api/v1/mobile/profile/me. `Profile` is null when the account has no linked participant profile.</summary>
public sealed class GetParticipantProfileResponse
{
    public bool HasProfileLink { get; set; }
    public ParticipantProfileDto? Profile { get; set; }
    public string Message { get; set; } = string.Empty;
}
