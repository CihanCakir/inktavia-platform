namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Profile;

/// <summary>Attach a completed client-side upload (fileId) as the participant avatar. The image bytes were PUT
/// directly to storage via /mobile/uploads — this carries only the fileId.</summary>
public sealed class UploadParticipantAvatarRequest
{
    public Guid FileId { get; set; }
}
