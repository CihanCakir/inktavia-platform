using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Vessel.Abstraction.Request.Document;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>
/// Attach a completed client-side upload to the vessel as a typed document (M4e, retrofitted to the M4f client-side
/// presigned flow). The bytes were PUT directly to storage via /mobile/uploads — the BFF only owner-gates and
/// attaches the fileId (the module validates the file + invalidates the documents default page). Returns the
/// created document with a freshly-resolved presigned read URL.
/// </summary>
public sealed class UploadMobileVesselDocumentCommandHandler
    : AizenCommandHandler<UploadMobileVesselDocumentCommand, MobileVesselDocumentDto>
{
    private const int OwnedPageIndex = 0;
    private const int OwnedPageSize = 20;

    private readonly IParticipantProfileResolver _resolver;
    private readonly IVesselRemoteCall _vessel;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<UploadMobileVesselDocumentCommandHandler> _logger;

    public UploadMobileVesselDocumentCommandHandler(
        IParticipantProfileResolver resolver,
        IVesselRemoteCall vessel,
        IFileStorageRemoteCall fileStorage,
        ILogger<UploadMobileVesselDocumentCommandHandler> logger)
    {
        _resolver = resolver;
        _vessel = vessel;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<MobileVesselDocumentDto?> Handle(
        UploadMobileVesselDocumentCommand request, CancellationToken cancellationToken)
    {
        if (request.FileId == Guid.Empty)
            throw new AizenBusinessException("A fileId is required.");
        if (string.IsNullOrWhiteSpace(request.DocumentTypeCode))
            throw new AizenBusinessException("A document type is required.");

        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        // Ownership gate (clean not-found).
        var ownedResp = await _vessel.GetUserVessels(OwnedPageIndex, OwnedPageSize);
        var owns = ownedResp?.Body?.Vessels?.Items?.Any(v => v.Id == request.VesselId) ?? false;
        if (!owns)
            throw new AizenBusinessException("Vessel not found.");

        // Attach the completed upload as a typed document (module validates the file + invalidates the default page).
        var documentName = string.IsNullOrWhiteSpace(request.DocumentName) ? "Document" : request.DocumentName!.Trim();

        var addResp = await _vessel.AddVesselDocument(request.VesselId, new AddVesselDocumentRequest
        {
            DocumentTypeCode = request.DocumentTypeCode.Trim(),
            DocumentName = documentName,
            FileId = request.FileId,
            ExpiresAt = request.ExpiresAt,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes!.Trim(),
        });

        var created = addResp?.Body?.Document
            ?? throw new AizenBusinessException("The document could not be attached to the vessel.");

        return await MobileVesselDocumentMapper.MapWithUrlAsync(created, _fileStorage, _logger, cancellationToken);
    }
}
