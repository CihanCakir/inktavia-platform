using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Participant;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Participant.ProvisionParticipantFromKeycloak;

public sealed class ProvisionParticipantFromKeycloakCommand : AizenCommand<ProvisionParticipantFromKeycloakResult>
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
