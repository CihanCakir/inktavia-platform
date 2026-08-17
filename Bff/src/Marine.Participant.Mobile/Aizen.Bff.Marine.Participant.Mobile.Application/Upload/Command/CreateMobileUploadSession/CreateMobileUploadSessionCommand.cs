using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Upload;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Upload;

/// <summary>POST /api/v1/mobile/uploads/session — the canonical client-side presigned upload primitive (M4f).
/// Creates a FileStorage session signed for the device-reachable public endpoint and returns the presigned PUT URL
/// (+ session code) to the client. The client PUTs the bytes directly to storage — they never traverse the BFF.</summary>
public sealed class CreateMobileUploadSessionCommand : AizenCommand<MobileUploadSessionResponse>
{
    public CreateMobileUploadSessionCommand(CreateMobileUploadSessionRequest request) => Request = request;

    public CreateMobileUploadSessionRequest Request { get; }
}
