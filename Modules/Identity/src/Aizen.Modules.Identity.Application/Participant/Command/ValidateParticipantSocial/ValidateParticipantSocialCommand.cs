using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Participant;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Participant.ValidateParticipantSocial;

public sealed class ValidateParticipantSocialCommand : AizenCommand<ParticipantSocialValidateResult>
{
    public string Provider { get; set; } = default!;   // "google" | "apple"
    public string IdToken { get; set; } = default!;
    public string? Nonce { get; set; }
    /// <summary>Apple only sends the name on FIRST authorization and never in the id_token, so the BFF passes
    /// it through; used as a fallback when the token carries no name.</summary>
    public string? FullName { get; set; }
}
