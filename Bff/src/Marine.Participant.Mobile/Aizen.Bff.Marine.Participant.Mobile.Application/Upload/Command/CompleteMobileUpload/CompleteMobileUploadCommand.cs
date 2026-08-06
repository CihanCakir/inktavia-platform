using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Upload;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Upload;

/// <summary>POST /api/v1/mobile/uploads/complete — finalize a client-side presigned upload after the client's PUT.
/// The module verifies the object exists in storage and marks the file Ready; returns the committed fileId.</summary>
public sealed class CompleteMobileUploadCommand : AizenCommand<MobileUploadCompleteResponse>
{
    public CompleteMobileUploadCommand(string uploadSessionCode) => UploadSessionCode = uploadSessionCode;

    public string UploadSessionCode { get; }
}
