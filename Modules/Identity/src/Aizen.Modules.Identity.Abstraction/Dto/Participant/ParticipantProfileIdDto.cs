namespace Aizen.Modules.Identity.Abstraction.Dto.Participant;

/// <summary>
/// BE_NF1b — maps a participant USER id to its participant PROFILE id (the <c>UserProfiles</c> row where
/// <c>RoleContext = Participant</c>). Consumed by the Notification module so owner-facing notifications are filed under
/// the profile id — the id the owner's mobile inbox + device tokens resolve to (symmetric with the provider, which uses
/// its ProviderProfileId). <see cref="ProfileId"/> is 0 when the user has no participant profile.
/// </summary>
public sealed class ParticipantProfileIdDto
{
    public long UserId { get; set; }
    public long ProfileId { get; set; }
}
