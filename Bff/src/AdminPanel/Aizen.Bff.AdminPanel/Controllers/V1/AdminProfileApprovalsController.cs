using Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Command;
using Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;
using Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Query;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/admin-panel/users/profile-approvals")]
[Tags("Admin Panel - Profile Approvals")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class AdminProfileApprovalsController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public AdminProfileApprovalsController(
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
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new GetProfileApprovalQueueBffQuery(
                userToken, pageIndex, pageSize, searchTerm, profileType,
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
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new GetOrganizerApprovalDetailBffQuery(userId, profileId, userToken), ct);
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
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new GetVenueApprovalDetailBffQuery(userId, profileId, userToken), ct);
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
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new ApproveOrganizerProfileBffCommand(userId, profileId, userToken), ct);
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
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new RejectOrganizerProfileBffCommand(userId, profileId, request?.Reason ?? string.Empty, userToken), ct);
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
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new ApproveVenueProfileBffCommand(userId, profileId, userToken), ct);
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
        var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
        var result = await _cqrs.ProcessAsync(
            new RejectVenueProfileBffCommand(userId, profileId, request?.Reason ?? string.Empty, userToken), ct);
        return SetResponse(result);
    }
}
