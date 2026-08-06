namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Profile;

/// <summary>
/// Body of PUT /api/v1/mobile/profile/me — the participant-editable fields, matching what the Identity
/// participant-profile update supports. Email and Phone are intentionally NOT here: they are auth identifiers
/// (phone is the OTP/login handle) and are read-only in M3a. Avatar (ProfilePhotoUrl) is wired in M3c.
/// </summary>
public sealed class UpdateParticipantProfileBffRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Bio { get; set; }
    public string? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? NationalityId { get; set; }
}
