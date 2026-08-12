using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Participant;

namespace Aizen.Modules.Identity.Application.ParticipantLookup.GetParticipantProfileIdByUserId;

/// <summary>
/// BE_NF1b — resolve a participant USER id to its participant PROFILE id (UserProfiles where RoleContext=Participant).
/// Internal read-model used by the Notification module (owner-notification recipient-key alignment). Ids only, no PII.
/// </summary>
public sealed class GetParticipantProfileIdByUserIdQuery : AizenQuery<ParticipantProfileIdDto>
{
    public long UserId { get; init; }
}
