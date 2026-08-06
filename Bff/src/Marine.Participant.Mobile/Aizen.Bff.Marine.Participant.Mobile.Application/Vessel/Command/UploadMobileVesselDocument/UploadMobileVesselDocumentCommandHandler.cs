using System.Net.Http.Headers;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.FileStorage.Abstraction.Enum;
using Aizen.Modules.FileStorage.Abstraction.Request.UploadSession;
using Aizen.Modules.Vessel.Abstraction.Request.Document;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>
/// Server-side vessel-document upload (M4e; mirrors the M3c avatar flow). Resolve the participant → owner-gate the
/// vessel → create a ServerSideUpload FileStorage session (presigned PUT signed for the internal S3 endpoint) →
/// PUT the bytes with a plain client → complete the session → attach the resulting FileId to the vessel as a
/// typed document via the module (which invalidates the documents default page). Returns the created document with
/// a freshly-resolved presigned read URL.
/// </summary>
public sealed class UploadMobileVesselDocumentCommandHandler
    : AizenCommandHandler<UploadMobileVesselDocumentCommand, MobileVesselDocumentDto>
{
    private const int OwnedPageIndex = 0;
    private const int OwnedPageSize = 20;

    private readonly IParticipantProfileResolver _resolver;
    private readonly IVesselRemoteCall _vessel;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<UploadMobileVesselDocumentCommandHandler> _logger;

    public UploadMobileVesselDocumentCommandHandler(
        IParticipantProfileResolver resolver,
        IVesselRemoteCall vessel,
        IFileStorageRemoteCall fileStorage,
        IHttpClientFactory httpFactory,
        ILogger<UploadMobileVesselDocumentCommandHandler> logger)
    {
        _resolver = resolver;
        _vessel = vessel;
        _fileStorage = fileStorage;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public override async Task<MobileVesselDocumentDto?> Handle(
        UploadMobileVesselDocumentCommand request, CancellationToken cancellationToken)
    {
        if (request.Content is null || request.Content.Length == 0)
            throw new AizenBusinessException("No document file was provided.");
        if (string.IsNullOrWhiteSpace(request.DocumentTypeCode))
            throw new AizenBusinessException("A document type is required.");

        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        // Ownership gate (clean not-found) before touching storage.
        var ownedResp = await _vessel.GetUserVessels(OwnedPageIndex, OwnedPageSize);
        var owns = ownedResp?.Body?.Vessels?.Items?.Any(v => v.Id == request.VesselId) ?? false;
        if (!owns)
            throw new AizenBusinessException("Vessel not found.");

        // 1) ServerSideUpload session (presigned PUT signed for the internal S3 endpoint).
        var sessionResp = await _fileStorage.CreateUploadSession(new CreateUploadSessionRequest
        {
            OriginalFileName = string.IsNullOrWhiteSpace(request.FileName) ? "document" : request.FileName,
            ContentType = string.IsNullOrWhiteSpace(request.ContentType) ? "application/octet-stream" : request.ContentType,
            SizeInBytes = request.SizeInBytes > 0 ? request.SizeInBytes : request.Content.LongLength,
            Category = FileCategory.Document,
            Visibility = FileVisibility.Private,
            OwnerModule = "Vessel",
            OwnerEntityType = "VesselDocument",
            ServerSideUpload = true,
        });
        var session = sessionResp?.Body ?? throw new AizenBusinessException("Could not start the document upload.");

        // 2) Push the bytes to the presigned URL (plain client — the URL carries its own S3 signature).
        await UploadBytesAsync(session.UploadUrl, request.Content, request.ContentType, cancellationToken);

        // 3) Finalize the upload → committed FileId.
        var completed = await _fileStorage.CompleteUploadSession(session.UploadSessionCode, new CompleteUploadSessionRequest());
        var fileId = completed?.Body?.FileId ?? session.FileId;

        // 4) Attach to the vessel as a typed document (module invalidates the documents default page).
        var documentName = string.IsNullOrWhiteSpace(request.DocumentName)
            ? (string.IsNullOrWhiteSpace(request.FileName) ? "Document" : request.FileName!.Trim())
            : request.DocumentName!.Trim();

        var addResp = await _vessel.AddVesselDocument(request.VesselId, new AddVesselDocumentRequest
        {
            DocumentTypeCode = request.DocumentTypeCode.Trim(),
            DocumentName = documentName,
            FileId = fileId,
            ExpiresAt = request.ExpiresAt,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes!.Trim(),
        });

        var created = addResp?.Body?.Document
            ?? throw new AizenBusinessException("Document uploaded but could not be attached to the vessel.");

        // 5) Return the created doc with a fresh presigned read URL.
        return await MobileVesselDocumentMapper.MapWithUrlAsync(created, _fileStorage, _logger, cancellationToken);
    }

    private async Task UploadBytesAsync(string url, byte[] content, string? contentType, CancellationToken ct)
    {
        var client = _httpFactory.CreateClient(); // no delegating handler → no Authorization on the S3 PUT
        using var body = new ByteArrayContent(content);
        body.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

        using var resp = await client.PutAsync(url, body, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Vessel document S3 PUT failed {Status}: {Body}", (int)resp.StatusCode, text);
            throw new AizenBusinessException("Document upload to storage failed. Please try again.");
        }
    }
}
