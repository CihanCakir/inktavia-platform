using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Common;

namespace Aizen.Modules.Identity.Application.ParticipantLookup.GetProfileContactEmail;

/// <summary>
/// BE_NF2 — resolve a profile's contact email by <c>UserProfiles.Id</c> (participant or organizer profile). Internal
/// read-model used by the Notification module to address an Email-channel delivery. Email only, no other PII.
/// </summary>
public sealed class GetProfileContactEmailByProfileIdQuery : AizenQuery<ProfileContactEmailDto>
{
    public long ProfileId { get; init; }
}
