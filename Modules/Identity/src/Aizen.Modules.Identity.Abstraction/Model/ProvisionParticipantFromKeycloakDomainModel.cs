namespace Aizen.Modules.Identity.Abstraction.Model;

/// <summary>
/// Input for idempotently provisioning/linking a Participant (mobile) profile from a Keycloak-authenticated
/// user. No password is involved — Keycloak is the authentication authority.
/// </summary>
public sealed class ProvisionParticipantFromKeycloakDomainModel
{
    public string KeycloakSubjectId { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ContactPhone { get; set; }
    public bool? EmailVerified { get; set; }

    // Optional (M2d social): record an idempotent external-login link for this participant.
    public string? ExternalProvider { get; set; }        // "google" | "apple"
    public string? ExternalProviderUserId { get; set; }  // social id_token.sub
}
