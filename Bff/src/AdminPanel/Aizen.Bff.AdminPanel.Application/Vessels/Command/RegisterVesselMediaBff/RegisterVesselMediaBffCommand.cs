using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Response.Media;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Command;

[DocumentationInfo("Register vessel media BFF command", "Completes the upload session then registers a new vessel media item with the uploaded FileId (B3).")]
public sealed class RegisterVesselMediaBffCommand : AizenCommand<AddVesselMediaResponse>
{
    public long VesselId { get; }
    public string FileId { get; }               // FileStorage FileId (Guid as string)
    public string UploadSessionCode { get; }
    public VesselMediaType MediaType { get; }
    public bool IsCover { get; }
    public int SortOrder { get; }

    public RegisterVesselMediaBffCommand(
        long vesselId, string fileId, string uploadSessionCode, VesselMediaType mediaType, bool isCover, int sortOrder)
    {
        VesselId = vesselId;
        FileId = fileId;
        UploadSessionCode = uploadSessionCode;
        MediaType = mediaType;
        IsCover = isCover;
        SortOrder = sortOrder;
    }
}
