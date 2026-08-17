using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Command;

[DocumentationInfo("Request vessel file upload URL BFF command", "Requests a pre-signed PUT URL from FileStorage for a vessel document/media upload (B3/B4).")]
public sealed class RequestVesselFileUploadUrlBffCommand : AizenCommand<VesselFileUploadUrlBffResponse>
{
    public long VesselId { get; }
    public string FileName { get; }
    public string ContentType { get; }
    public long FileSizeBytes { get; }

    public RequestVesselFileUploadUrlBffCommand(long vesselId, string fileName, string contentType, long fileSizeBytes)
    {
        VesselId = vesselId;
        FileName = fileName;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
    }
}
