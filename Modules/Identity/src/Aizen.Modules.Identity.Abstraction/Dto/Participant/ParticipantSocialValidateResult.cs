namespace Aizen.Modules.Identity.Abstraction.Dto.Participant;

/// <summary>Validated claims from a native Google/Apple id_token (signature/iss/aud/exp verified).</summary>
public sealed class ParticipantSocialValidateResult
{
    public string Provider { get; set; } = default!;        // "google" | "apple"
    public string ProviderUserId { get; set; } = default!;  // id_token.sub
    public string? Email { get; set; }
    public bool EmailVerified { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
