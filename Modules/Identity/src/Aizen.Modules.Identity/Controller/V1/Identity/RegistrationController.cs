using Aizen.Modules.InktaviaStore.Application.Identity.Command.RegisterOrganizer;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.RegisterVenue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Aizen.Core.Infrastructure.Api;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.RegisterParticipant;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.Participant;
using Aizen.Modules.InktaviaStore.Application.Identity.Command;
using Aizen.Modules.Identity.Abstraction.Response;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Request;

namespace Aizen.Modules.InktaviaStore.Controller.V1.Identity
{

    [ApiController]
    [Route("api/v1/identity")]
    [Tags("Identity - Registration")]
    public sealed class RegistrationController : AizenWebApiController
    {
        private readonly IAizenCQRSProcessor _sender;
        public RegistrationController(
            IHttpContextAccessor httpContextAccessor,
            IAizenCQRSProcessor cqrsProcessor) : base(httpContextAccessor)
        {
            _sender = cqrsProcessor;
        }

        // =========================
        // PARTICIPANT — OAUTH START
        // GET /api/v1/identity/participant/oauth/start?provider=google|apple&redirect=/&lang=tr
        // =========================
        [HttpGet("participant/oauth/start")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(StartExternalLoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<AizenApiResponse<StartExternalLoginResponse>> StartExternalLoginParticipant(
            [FromQuery] string provider,
            [FromQuery] string? redirect,
            [FromQuery] string? lang,
            CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(
                new StartExternalLoginParticipantCommand(
                    provider: provider,
                    redirectAfterLogin: redirect,
                    uiLocale: lang
                ),
                ct);

            return SetResponse(result);
        }

        // ===========================
        // PARTICIPANT — OAUTH COMPLETE (CALLBACK)
        // POST /api/v1/identity/participant/oauth/callback/{provider}
        // Body(form): code, state
        // Header(ops): X-Device-Id, X-Notification-Token
        // ===========================
        [HttpPost("participant/oauth/callback/{provider}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(UserLoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<AizenApiResponse<UserLoginResponse>> CompleteExternalLoginParticipant(
            [FromRoute] string provider,
            [FromForm] string code,
            [FromForm] string state,
            CancellationToken ct)
        {
            // İsteğe göre Device bilgilerini header’dan alıyoruz
            var deviceId = HttpContext.Request.Headers["X-Device-Id"].ToString();
            var notifToken = HttpContext.Request.Headers["X-Notification-Token"].ToString();

            var result = await _sender.ProcessAsync(
                new CompleteExternalLoginParticipantCommand(
                    provider: provider,
                    code: code,
                    state: state,
                    codeVerifier: null, // Start'ta cache'lediğimiz için handler temp'ten okuyacak
                    deviceId: string.IsNullOrWhiteSpace(deviceId) ? null : deviceId,
                    deviceType: ConsumerDeviceType.Web,
                    notificationToken: string.IsNullOrWhiteSpace(notifToken) ? null : notifToken,
                    userAgent: Request.Headers.UserAgent.ToString(),
                    ip: HttpContext.Connection.RemoteIpAddress?.ToString()
                ),
                ct);

            return SetResponse(result);
        }


        // POST /api/v1/identity/participant/register
        [HttpPost("participant/register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RegisterResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<AizenApiResponse<RegisterResult>> RegisterParticipant(
            [FromBody] RegisterConsumerRequest req,
            CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(
                new RegisterParticipantCommand(
                    email: req.Email,
                    phone: req.Phone,
                    password: req.Password,
                    firstName: req.FirstName,
                    lastName: req.LastName,
                    kvkkAccepted: req.KvkkAccepted,
                    deviceId: req.DeviceId,
                    deviceType: req.DeviceType,
                    notificationToken: req.NotificationToken
                ), ct);

            return SetResponse(result);
        }

        // POST /api/v1/identity/organizers/register
        [HttpPost("organizers/register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RegisterResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<AizenApiResponse<RegisterResult>> RegisterOrganizer(
            [FromBody] RegisterOrganizerRequest req,
            CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(
                new RegisterOrganizerCommand(
                    email: req.Email,
                    companyName: req.CompanyName,
                    taxNo: req.TaxNo,
                    contactPhone: req.Phone,
                    password: req.Password,
                    ownerFirstName: req.OwnerFirstName,
                    ownerLastName: req.OwnerLastName,
                    kvkkAccepted: req.KvkkAccepted,
                    deviceId: req.DeviceId,
                    deviceType: req.DeviceType,
                    notificationToken: req.NotificationToken
                ), ct);

            return SetResponse(result);
        }

        // POST /api/v1/identity/venues/register
        [HttpPost("venues/register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RegisterResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<AizenApiResponse<RegisterResult>> RegisterVenue(
            [FromBody] RegisterVenueRequest req,
            CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(
                new RegisterVenueCommand(
                    venueName: req.VenueName,
                    address: req.Address,
                    email: req.Email,
                    contactPhone: req.Phone,
                    password: req.Password,
                    ownerFirstName: req.OwnerFirstName,
                    ownerLastName: req.OwnerLastName,
                    kvkkAccepted: req.KvkkAccepted,
                    deviceId: req.DeviceId,
                    deviceType: req.DeviceType,
                    notificationToken: req.NotificationToken
                ), ct);

            return SetResponse(result);
        }
    }

}