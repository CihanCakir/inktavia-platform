namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Profile;

/// <summary>
/// The participant's profile as surfaced to the mobile client. Mirrors the MarineProvider ProviderProfileDto
/// shape for the participant domain. Built from the Identity participant profile (resolved by Keycloak subject).
/// `FullName` = First + Last; `AvatarUrl` = the Identity ProfilePhotoUrl. Email/Phone are read-only here
/// (auth identifiers); avatar upload arrives in M3c.
/// </summary>
public sealed class ParticipantProfileDto
{
    public long ParticipantProfileId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? NationalityId { get; set; }
    public string? ApprovalStatus { get; set; }
    public string? ProfileStatus { get; set; }
}
