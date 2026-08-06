using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Profile;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Profile;

/// <summary>POST /api/v1/mobile/profile/avatar — attach an already-uploaded (client-side presigned + completed)
/// image as the participant's avatar. The bytes were PUT directly to storage via /mobile/uploads — never through
/// the BFF. Returns the updated profile with a fresh presigned avatar URL.</summary>
public sealed class UploadParticipantAvatarCommand : AizenCommand<GetParticipantProfileResponse>
{
    public Guid FileId { get; set; }
}
