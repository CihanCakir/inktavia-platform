using System.Security.Claims;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Identity.Abstraction.Request;
using Aizen.Modules.Identity.Abstraction.Response;
using Aizen.Modules.InktaviaStore.Application.Identity;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.Venue;
using Aizen.Modules.InktaviaStore.Application.Identity.Query.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.InktaviaStore.Controller.V1.Identity
{
    [ApiController]
    [Route("api/v1/identity")]
    [Tags("Identity - Admin")]
    [Authorize(Roles = RoleNames.Admin)]
    public sealed class AdminController : AizenWebApiController
    {
        private readonly IAizenCQRSProcessor _sender;

        public AdminController(
            IHttpContextAccessor httpContextAccessor,
            IAizenCQRSProcessor cqrsProcessor) : base(httpContextAccessor)
        {
            _sender = cqrsProcessor;
        }

        private string? GetCurrentUserEmail()
            => ContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value
            ?? ContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // POST /api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/approve
        [HttpPost("admin/organizers/{userId}/profiles/{profileId}/approve")]
        [ProducesResponseType(typeof(VenueOrganizationRegistrationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<AizenApiResponse<VenueOrganizationRegistrationResponse>> ApproveOrganizer(
            [FromRoute] long userId,
            [FromRoute] long profileId,
            CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(
                new ApproveOrganizerProfileCommand(userId, profileId, GetCurrentUserEmail()),
                ct);

            return SetResponse(result);
        }

        // POST /api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/reject
        [HttpPost("admin/organizers/{userId}/profiles/{profileId}/reject")]
        [ProducesResponseType(typeof(VenueOrganizationRegistrationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<AizenApiResponse<VenueOrganizationRegistrationResponse>> RejectOrganizer(
            [FromRoute] long userId,
            [FromRoute] long profileId,
            [FromBody] RejectProfileRequest req,
            CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(
                new RejectOrganizerProfileCommand(userId, profileId, req.Reason,
                    req.ReasonCategory, req.InternalNote, req.NotifyUser, GetCurrentUserEmail()),
                ct);

            return SetResponse(result);
        }

        // POST /api/v1/identity/admin/venues/{userId}/profiles/{profileId}/approve
        [HttpPost("admin/venues/{userId}/profiles/{profileId}/approve")]
        [ProducesResponseType(typeof(VenueOrganizationRegistrationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<AizenApiResponse<VenueOrganizationRegistrationResponse>> ApproveVenue(
            [FromRoute] long userId,
            [FromRoute] long profileId,
            CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(
                new ApproveVenueProfileCommand(userId, profileId, GetCurrentUserEmail()),
                ct);

            return SetResponse(result);
        }

        // POST /api/v1/identity/admin/venues/{userId}/profiles/{profileId}/reject
        [HttpPost("admin/venues/{userId}/profiles/{profileId}/reject")]
        [ProducesResponseType(typeof(VenueOrganizationRegistrationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<AizenApiResponse<VenueOrganizationRegistrationResponse>> RejectVenue(
            [FromRoute] long userId,
            [FromRoute] long profileId,
            [FromBody] RejectProfileRequest req,
            CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(
                new RejectVenueProfileCommand(userId, profileId, req.Reason,
                    req.ReasonCategory, req.InternalNote, req.NotifyUser, GetCurrentUserEmail()),
                ct);

            return SetResponse(result);
        }

        // POST /api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/documents
        [HttpPost("admin/organizers/{userId}/profiles/{profileId}/documents")]
        [ProducesResponseType(typeof(AddVerificationDocumentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<AizenApiResponse<AddVerificationDocumentResult>> AddOrganizerDocument(
            [FromRoute] long userId,
            [FromRoute] long profileId,
            [FromBody] AddVerificationDocumentRequest req,
            CancellationToken ct)
        {
            var adminUserId = long.TryParse(
                ContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0L;

            var result = await _sender.ProcessAsync(
                new AddOrganizerVerificationDocumentCommand(userId, profileId, req.FileId,
                    req.Name, req.DocumentType, req.Format, req.FileSizeDisplay, req.Issuer, adminUserId),
                ct);

            return SetResponse(result);
        }

        // POST /api/v1/identity/admin/venues/{userId}/profiles/{profileId}/documents
        [HttpPost("admin/venues/{userId}/profiles/{profileId}/documents")]
        [ProducesResponseType(typeof(AddVerificationDocumentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<AizenApiResponse<AddVerificationDocumentResult>> AddVenueDocument(
            [FromRoute] long userId,
            [FromRoute] long profileId,
            [FromBody] AddVerificationDocumentRequest req,
            CancellationToken ct)
        {
            var adminUserId = long.TryParse(
                ContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0L;

            var result = await _sender.ProcessAsync(
                new AddVenueVerificationDocumentCommand(userId, profileId, req.FileId,
                    req.Name, req.DocumentType, req.Format, req.FileSizeDisplay, req.Issuer, adminUserId),
                ct);

            return SetResponse(result);
        }

        // GET /api/v1/identity/admin/users/{userId}/login-history
        [HttpGet("admin/users/{userId}/login-history")]
        [ProducesResponseType(typeof(List<UserLoginHistoryItemDto>), StatusCodes.Status200OK)]
        public async Task<AizenApiResponse<List<UserLoginHistoryItemDto>>> GetUserLoginHistory(
            [FromRoute] long userId,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default)
        {
            var result = await _sender.ProcessAsync(
                new GetUserLoginHistoryQuery(userId, pageSize),
                ct);
            return SetResponse(result);
        }
    }
}
