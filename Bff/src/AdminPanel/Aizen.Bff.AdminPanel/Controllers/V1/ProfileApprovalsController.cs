using Aizen.Bff.AdminPanel.Application.ProfileApprovals.Command;
using Aizen.Bff.AdminPanel.Application.ProfileApprovals.Dto;
using Aizen.Bff.AdminPanel.Application.ProfileApprovals.Query;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel/users/profile-approvals")]
[Tags("Admin Panel - Profile Approvals")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class ProfileApprovalsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public ProfileApprovalsController(
        IHttpContextAccessor httpContextAccessor,
        IAizenCQRSProcessor cqrsProcessor)
        : base(httpContextAccessor)
    {
        _cqrs = cqrsProcessor;
    }

    /// <summary>
    /// Combined organizer + venue approval queue with filters and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AdminProfileApprovalQueueBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminProfileApprovalQueueBffResponse>> GetQueue(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? profileType = null,
        [FromQuery] string? status = null,
        [FromQuery] string? submittedFrom = null,
        [FromQuery] string? submittedTo = null,
        [FromQuery] string? riskLevel = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetProfileApprovalQueueBffQuery(
                pageIndex, pageSize, searchTerm, profileType,
                status, submittedFrom, submittedTo, riskLevel), ct);
        return SetResponse(result);
    }

    /// <summary>
    /// Full organizer profile review detail for the admin approval screen.
    /// </summary>
    [HttpGet("organizers/{userId:long}/profiles/{profileId:long}")]
    [ProducesResponseType(typeof(OrganizerApprovalDetailBffResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<AizenApiResponse<OrganizerApprovalDetailBffResponse>> GetOrganizerDetail(
        long userId, long profileId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetOrganizerApprovalDetailBffQuery(userId, profileId), ct);
        return SetResponse(result);
    }

    /// <summary>
    /// Full venue profile review detail for the admin approval screen.
    /// </summary>
    [HttpGet("venues/{userId:long}/profiles/{profileId:long}")]
    [ProducesResponseType(typeof(VenueApprovalDetailBffResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<AizenApiResponse<VenueApprovalDetailBffResponse>> GetVenueDetail(
        long userId, long profileId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new GetVenueApprovalDetailBffQuery(userId, profileId), ct);
        return SetResponse(result);
    }

    /// <summary>
    /// Approve an organizer profile.
    /// </summary>
    [HttpPost("organizers/{userId:long}/profiles/{profileId:long}/approve")]
    [ProducesResponseType(typeof(ProfileApprovalDecisionBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProfileApprovalDecisionBffResponse>> ApproveOrganizer(
        long userId, long profileId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new ApproveOrganizerProfileBffCommand(userId, profileId), ct);
        return SetResponse(result);
    }

    /// <summary>
    /// Reject an organizer profile. Reason is required (10–1000 chars).
    /// </summary>
    [HttpPost("organizers/{userId:long}/profiles/{profileId:long}/reject")]
    [ProducesResponseType(typeof(ProfileApprovalDecisionBffResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<AizenApiResponse<ProfileApprovalDecisionBffResponse>> RejectOrganizer(
        long userId, long profileId,
        [FromBody] RejectProfileApprovalBffRequest request,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new RejectOrganizerProfileBffCommand(userId, profileId, request?.Reason ?? string.Empty), ct);
        return SetResponse(result);
    }

    /// <summary>
    /// Approve a venue profile.
    /// </summary>
    [HttpPost("venues/{userId:long}/profiles/{profileId:long}/approve")]
    [ProducesResponseType(typeof(ProfileApprovalDecisionBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ProfileApprovalDecisionBffResponse>> ApproveVenue(
        long userId, long profileId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new ApproveVenueProfileBffCommand(userId, profileId), ct);
        return SetResponse(result);
    }

    /// <summary>
    /// Reject a venue profile. Reason is required (10–1000 chars).
    /// </summary>
    [HttpPost("venues/{userId:long}/profiles/{profileId:long}/reject")]
    [ProducesResponseType(typeof(ProfileApprovalDecisionBffResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<AizenApiResponse<ProfileApprovalDecisionBffResponse>> RejectVenue(
        long userId, long profileId,
        [FromBody] RejectProfileApprovalBffRequest request,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new RejectVenueProfileBffCommand(userId, profileId, request?.Reason ?? string.Empty), ct);
        return SetResponse(result);
    }

    // ── Onboarding Revision ───────────────────────────────────────────────────

    /// <summary>
    /// Admin requests the provider to revise specific onboarding steps.
    /// At least one step is required; note must be 10–2000 characters.
    /// </summary>
    [HttpPost("organizers/{userId:long}/profiles/{profileId:long}/revision")]
    [ProducesResponseType(typeof(OnboardingRevisionBffResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<AizenApiResponse<OnboardingRevisionBffResponse>> RequestOrganizerOnboardingRevision(
        long userId, long profileId,
        [FromBody] RequestOnboardingRevisionRequest request,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new RequestOrganizerOnboardingRevisionBffCommand(
                userId, profileId, request?.Steps ?? Array.Empty<string>(), request?.Note ?? string.Empty), ct);
        return SetResponse(result);
    }

    // ── Document Upload Flow ─────────────────────────────────────────────────

    /// <summary>
    /// Request a pre-signed PUT URL for uploading an organizer verification document directly to storage.
    /// The browser uses the returned putUrl to PUT the file. Then call the register document endpoint.
    /// </summary>
    [HttpPost("organizers/{userId:long}/profiles/{profileId:long}/documents/upload-url")]
    [ProducesResponseType(typeof(DocumentUploadUrlBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<DocumentUploadUrlBffResponse>> RequestOrganizerDocumentUploadUrl(
        long userId, long profileId,
        [FromBody] RequestDocumentUploadUrlRequest request,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new RequestOrganizerDocumentUploadUrlBffCommand(
                userId, profileId,
                request.FileName, request.ContentType, request.FileSizeBytes, request.DocumentType), ct);
        return SetResponse(result);
    }

    /// <summary>
    /// Complete the upload session and register an organizer verification document with Identity.
    /// Call this after the browser has PUT the file to the pre-signed URL.
    /// </summary>
    [HttpPost("organizers/{userId:long}/profiles/{profileId:long}/documents")]
    [ProducesResponseType(typeof(RegisterDocumentBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RegisterDocumentBffResponse>> RegisterOrganizerDocument(
        long userId, long profileId,
        [FromBody] RegisterDocumentRequest request,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new RegisterOrganizerVerificationDocumentBffCommand(
                userId, profileId,
                request.FileId, request.UploadSessionCode,
                request.DocumentType, request.Name,
                request.Format, request.FileSizeDisplay, request.Issuer), ct);
        return SetResponse(result);
    }

    /// <summary>
    /// Request a pre-signed PUT URL for uploading a venue verification document directly to storage.
    /// </summary>
    [HttpPost("venues/{userId:long}/profiles/{profileId:long}/documents/upload-url")]
    [ProducesResponseType(typeof(DocumentUploadUrlBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<DocumentUploadUrlBffResponse>> RequestVenueDocumentUploadUrl(
        long userId, long profileId,
        [FromBody] RequestDocumentUploadUrlRequest request,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new RequestVenueDocumentUploadUrlBffCommand(
                userId, profileId,
                request.FileName, request.ContentType, request.FileSizeBytes, request.DocumentType), ct);
        return SetResponse(result);
    }

    /// <summary>
    /// Complete the upload session and register a venue verification document with Identity.
    /// </summary>
    [HttpPost("venues/{userId:long}/profiles/{profileId:long}/documents")]
    [ProducesResponseType(typeof(RegisterDocumentBffResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RegisterDocumentBffResponse>> RegisterVenueDocument(
        long userId, long profileId,
        [FromBody] RegisterDocumentRequest request,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync(
            new RegisterVenueVerificationDocumentBffCommand(
                userId, profileId,
                request.FileId, request.UploadSessionCode,
                request.DocumentType, request.Name,
                request.Format, request.FileSizeDisplay, request.Issuer), ct);
        return SetResponse(result);
    }
}

// ── Controller request models ─────────────────────────────────────────────────

public sealed class RequestDocumentUploadUrlRequest
{
    [Required] public string FileName { get; set; } = null!;
    [Required] public string ContentType { get; set; } = null!;
    [Range(1, 10 * 1024 * 1024)] public long FileSizeBytes { get; set; }
    [Required] public string DocumentType { get; set; } = null!;
}

public sealed class RegisterDocumentRequest
{
    [Required] public string FileId { get; set; } = null!;           // FileStorage FileId (Guid as string)
    [Required] public string UploadSessionCode { get; set; } = null!;
    [Required] public string DocumentType { get; set; } = null!;
    [Required] public string Name { get; set; } = null!;
    public string? Format { get; set; }
    public string? FileSizeDisplay { get; set; }
    public string? Issuer { get; set; }
}

public sealed class RequestOnboardingRevisionRequest
{
    /// <summary>camelCase step names: businessIdentity | serviceCapabilities | operatingRegion | complianceVerification | cargoDryInterest</summary>
    [Required] public string[] Steps { get; set; } = Array.Empty<string>();

    /// <summary>Admin note explaining what needs to be fixed. 10–2000 chars.</summary>
    [Required] public string Note { get; set; } = null!;
}
