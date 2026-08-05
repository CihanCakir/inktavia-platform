using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Aizen.Modules.Identity.Abstraction.Dto.Participant;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.Participant.ProvisionParticipantFromKeycloak;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.Participant.ValidateParticipantSocial;
using Aizen.Modules.InktaviaStore.Application.Identity.Query.Participant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.InktaviaStore.Controller.V1.Identity
{
    /// <summary>
    /// Keycloak ↔ Identity participant-link endpoints (mirrors <c>ProviderLinkController</c>). Service-token
    /// authorized (IdentityRead/IdentityWrite), callable by the marine-mobile-bff service account. Not anonymous.
    /// </summary>
    [ApiController]
    [Route("api/v1/identity")]
    [Tags("Identity - Participant Link")]
    public sealed class ParticipantLinkController : AizenWebApiController
    {
        private readonly IAizenCQRSProcessor _sender;

        public ParticipantLinkController(
            IHttpContextAccessor httpContextAccessor,
            IAizenCQRSProcessor cqrsProcessor) : base(httpContextAccessor)
        {
            _sender = cqrsProcessor;
        }

        // GET /api/v1/identity/participant/profiles/by-subject/{keycloakSubject}
        [Authorize(Policy = "IdentityRead")]
        [HttpGet("participant/profiles/by-subject/{keycloakSubject}")]
        [ProducesResponseType(typeof(OrganizerProfileDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<AizenApiResponse<OrganizerProfileDetailDto>> GetParticipantProfileByKeycloakSubject(
            [FromRoute] string keycloakSubject, CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(
                new GetParticipantProfileByKeycloakSubjectQuery(keycloakSubject), ct);

            return SetResponse(result);
        }

        // POST /api/v1/identity/auth/participant-social-validate — validate a native Google/Apple id_token.
        [Authorize(Policy = "IdentityWrite")]
        [HttpPost("auth/participant-social-validate")]
        [ProducesResponseType(typeof(ParticipantSocialValidateResult), StatusCodes.Status200OK)]
        public async Task<AizenApiResponse<ParticipantSocialValidateResult>> ValidateSocial(
            [FromBody] ValidateParticipantSocialCommand command, CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(command, ct);
            return SetResponse(result);
        }

        // POST /api/v1/identity/participant/provision-from-keycloak
        [Authorize(Policy = "IdentityWrite")]
        [HttpPost("participant/provision-from-keycloak")]
        [ProducesResponseType(typeof(ProvisionParticipantFromKeycloakResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<AizenApiResponse<ProvisionParticipantFromKeycloakResult>> ProvisionFromKeycloak(
            [FromBody] ProvisionParticipantFromKeycloakCommand command, CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(command, ct);
            return SetResponse(result);
        }
    }
}
