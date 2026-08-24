using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Common;

namespace Aizen.Modules.Identity.Application.ParticipantLookup.GetProfilePreferredLanguage;

/// <summary>
/// Locale — resolve a profile's persisted preferred language by <c>UserProfiles.Id</c> (participant or organizer
/// profile). Internal read-model used by the Notification module. Language only, no other data.
/// </summary>
public sealed class GetProfilePreferredLanguageByProfileIdQuery : AizenQuery<ProfilePreferredLanguageDto>
{
    public long ProfileId { get; init; }
}
