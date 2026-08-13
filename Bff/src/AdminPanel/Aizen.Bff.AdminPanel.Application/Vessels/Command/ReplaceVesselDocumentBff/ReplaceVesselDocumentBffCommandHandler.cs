using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Request.Document;
using Aizen.Modules.Vessel.Abstraction.Response.Document;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Command;

[DocumentationInfo("Replace vessel document BFF command handler", "Completes the FileStorage upload session, then swaps the document's file (Vessel module Update = new version).")]
public sealed class ReplaceVesselDocumentBffCommandHandler
    : AizenCommandHandler<ReplaceVesselDocumentBffCommand, UpdateVesselDocumentResponse>
{
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly IVesselRemoteCall _vessel;

    public ReplaceVesselDocumentBffCommandHandler(IFileStorageRemoteCall fileStorage, IVesselRemoteCall vessel)
    {
        _fileStorage = fileStorage;
        _vessel = vessel;
    }

    public override async Task<UpdateVesselDocumentResponse?> Handle(
        ReplaceVesselDocumentBffCommand request, CancellationToken cancellationToken)
    {
        var complete = await _fileStorage.CompleteDocumentUploadSession(
            request.UploadSessionCode, new CompleteDocumentUploadSessionRequest());
        if (complete?.Header?.IsSuccess != true)
            throw new InvalidOperationException(complete?.Header?.ErrorMessage ?? "Upload completion failed.");

        var result = await _vessel.UpdateVesselDocument(request.VesselId, request.DocumentId, new UpdateVesselDocumentRequest
        {
            DocumentName = request.DocumentName,
            DocumentTypeCode = request.DocumentTypeCode,
            FileId = Guid.Parse(request.FileId),
            ExpiresAt = request.ExpiresAt,
            Notes = request.Notes,
        });
        return result.Body;
    }
}
