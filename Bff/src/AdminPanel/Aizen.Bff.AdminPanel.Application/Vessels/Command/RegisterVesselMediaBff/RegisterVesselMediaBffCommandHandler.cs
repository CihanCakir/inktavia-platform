using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Request.Media;
using Aizen.Modules.Vessel.Abstraction.Response.Media;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Command;

[DocumentationInfo("Register vessel media BFF command handler", "Completes the FileStorage upload session, then registers the media (FileId) with the Vessel module.")]
public sealed class RegisterVesselMediaBffCommandHandler
    : AizenCommandHandler<RegisterVesselMediaBffCommand, AddVesselMediaResponse>
{
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly IVesselRemoteCall _vessel;

    public RegisterVesselMediaBffCommandHandler(IFileStorageRemoteCall fileStorage, IVesselRemoteCall vessel)
    {
        _fileStorage = fileStorage;
        _vessel = vessel;
    }

    public override async Task<AddVesselMediaResponse?> Handle(
        RegisterVesselMediaBffCommand request, CancellationToken cancellationToken)
    {
        var complete = await _fileStorage.CompleteDocumentUploadSession(
            request.UploadSessionCode, new CompleteDocumentUploadSessionRequest());
        if (complete?.Header?.IsSuccess != true)
            throw new InvalidOperationException(complete?.Header?.ErrorMessage ?? "Upload completion failed.");

        var result = await _vessel.AddVesselMedia(request.VesselId, new AddVesselMediaRequest
        {
            MediaType = request.MediaType,
            FileId = Guid.Parse(request.FileId),
            IsCover = request.IsCover,
            SortOrder = request.SortOrder,
        });
        return result.Body;
    }
}
