namespace Aizen.Modules.Identity.Abstraction.Dto.Common;

/// <summary>
/// BE_NF2 — the contact email of a profile's linked user, keyed by <c>UserProfiles.Id</c> (works for both a
/// participant profile and a provider/organizer profile — both are UserProfiles rows). Used by the Notification module
/// to address an <c>Email</c>-channel delivery to the same recipient the InApp notification is filed under. Internal
/// read only. <see cref="Email"/> is null when the profile or its user has no email.
/// </summary>
public sealed class ProfileContactEmailDto
{
    public long ProfileId { get; set; }
    public string? Email { get; set; }
}
