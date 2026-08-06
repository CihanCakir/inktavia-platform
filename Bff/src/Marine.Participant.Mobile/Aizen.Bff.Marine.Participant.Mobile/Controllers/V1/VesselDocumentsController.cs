using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Authorization;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.Marine.Participant.Mobile.Controllers.V1;

/// <summary>Participant-scoped vessel documents (M4e). List (with presigned URLs) + server-side upload + delete;
/// all owner-gated to the caller's own vessels (a foreign/unknown id yields a clean not-found).</summary>
[ApiController]
[Route("api/v1/mobile/vessels/{vesselId:long}/documents")]
[Tags("Mobile - Vessel Documents")]
[Authorize(Policy = ParticipantAuthorizationPolicies.ParticipantAuthenticated)]
public sealed class VesselDocumentsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public VesselDocumentsController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>The caller's documents for this vessel (each with a fresh presigned read URL). Fresh immediately
    /// after upload/delete (the BFF reads the module page that add/remove invalidate).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MobileVesselDocumentDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<MobileVesselDocumentDto>>> GetDocuments(
        [FromRoute] long vesselId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileVesselDocumentsQuery(vesselId), ct);
        return SetResponse(result);
    }

    /// <summary>Upload a document (multipart `file` + DOCUMENT_TYPE metadata). Server-side FileStorage upload →
    /// attach to the vessel; returns the created document with a presigned read URL.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MobileVesselDocumentDto), StatusCodes.Status200OK)]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<AizenApiResponse<MobileVesselDocumentDto>> UploadDocument(
        [FromRoute] long vesselId,
        [FromForm] IFormFile file,
        [FromForm] string documentTypeCode,
        [FromForm] string? documentName,
        [FromForm] DateTime? expiresAt,
        [FromForm] string? notes,
        CancellationToken ct)
    {
        using var ms = new MemoryStream();
        if (file is not null) await file.CopyToAsync(ms, ct);

        var result = await _cqrs.ProcessAsync(new UploadMobileVesselDocumentCommand
        {
            VesselId = vesselId,
            Content = ms.ToArray(),
            FileName = file?.FileName ?? "document",
            ContentType = file?.ContentType ?? "application/octet-stream",
            SizeInBytes = file?.Length ?? 0,
            DocumentTypeCode = documentTypeCode,
            DocumentName = documentName,
            ExpiresAt = expiresAt,
            Notes = notes,
        }, ct);
        return SetResponse(result);
    }

    /// <summary>Delete one of the caller's vessel documents; returns the deleted document id.</summary>
    [HttpDelete("{documentId:long}")]
    [ProducesResponseType(typeof(MobileVesselDocumentDeletedDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileVesselDocumentDeletedDto>> DeleteDocument(
        [FromRoute] long vesselId, [FromRoute] long documentId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new DeleteMobileVesselDocumentCommand(vesselId, documentId), ct);
        return SetResponse(result);
    }
}
