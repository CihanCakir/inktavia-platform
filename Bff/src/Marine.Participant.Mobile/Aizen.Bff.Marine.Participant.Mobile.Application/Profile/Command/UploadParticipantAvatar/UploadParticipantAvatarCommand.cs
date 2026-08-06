using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Profile;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Profile;

/// <summary>POST /api/v1/mobile/profile/avatar — the raw image bytes + metadata; returns the updated profile.</summary>
public sealed class UploadParticipantAvatarCommand : AizenCommand<GetParticipantProfileResponse>
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = "avatar";
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeInBytes { get; set; }
}
