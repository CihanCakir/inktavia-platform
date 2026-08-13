using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Request.Document;
using Aizen.Modules.Vessel.Abstraction.Response.Document;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Command;

[DocumentationInfo("Register vessel document BFF command handler", "Completes the FileStorage upload session, then registers the document (FileId) with the Vessel module.")]
public sealed class RegisterVesselDocumentBffCommandHandler
    : AizenCommandHandler<RegisterVesselDocumentBffCommand, AddVesselDocumentResponse>
{
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly IVesselRemoteCall _vessel;

    public RegisterVesselDocumentBffCommandHandler(IFileStorageRemoteCall fileStorage, IVesselRemoteCall vessel)
    {
        _fileStorage = fileStorage;
        _vessel = vessel;
    }

    public override async Task<AddVesselDocumentResponse?> Handle(
        RegisterVesselDocumentBffCommand request, CancellationToken cancellationToken)
    {
        // Step 1: confirm the file physically landed in storage.
        var complete = await _fileStorage.CompleteDocumentUploadSession(
            request.UploadSessionCode, new CompleteDocumentUploadSessionRequest());
        if (complete?.Header?.IsSuccess != true)
            throw new InvalidOperationException(complete?.Header?.ErrorMessage ?? "Upload completion failed.");

        // Step 2: register the document with the Vessel module.
        var result = await _vessel.AddVesselDocument(request.VesselId, new AddVesselDocumentRequest
        {
            DocumentTypeCode = request.DocumentTypeCode,
            DocumentName = request.DocumentName,
            FileId = Guid.Parse(request.FileId),
            ExpiresAt = request.ExpiresAt,
            Notes = request.Notes,
        });
        return result.Body;
    }
}
