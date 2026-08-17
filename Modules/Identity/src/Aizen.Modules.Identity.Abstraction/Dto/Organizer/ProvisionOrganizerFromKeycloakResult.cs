namespace Aizen.Modules.Identity.Abstraction.Dto.Organizer;

public sealed class ProvisionOrganizerFromKeycloakResult
{
    public long ProviderProfileId { get; set; }
    public long UserId { get; set; }
    public bool CreatedUser { get; set; }
    public bool CreatedProfile { get; set; }
    public bool AlreadyLinked { get; set; }
    public string ApprovalStatus { get; set; } = default!;
    public string ProfileStatus { get; set; } = default!;
    public List<string> Warnings { get; set; } = new();
}
