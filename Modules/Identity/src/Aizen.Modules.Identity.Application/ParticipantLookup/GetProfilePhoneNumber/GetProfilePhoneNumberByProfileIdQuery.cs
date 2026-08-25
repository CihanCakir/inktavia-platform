using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Common;

namespace Aizen.Modules.Identity.Application.ParticipantLookup.GetProfilePhoneNumber;

/// <summary>Faz 28.7 — profil id → telefon (UserProfiles→Users.PhoneNumber). E-posta çözümüyle aynı sınır.</summary>
public sealed class GetProfilePhoneNumberByProfileIdQuery : AizenQuery<ProfilePhoneNumberDto>
{
    public long ProfileId { get; init; }
}
