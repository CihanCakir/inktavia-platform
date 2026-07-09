using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer.MarkOrganizerPhoneVerified;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer.ProvisionOrganizerFromKeycloak;
using Aizen.Modules.InktaviaStore.Application.Identity.Query.Organizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.InktaviaStore.Controller.V1.Identity
{
    /// <summary>
    /// Keycloak ↔ Identity provider-link endpoints. Service-token authorized (IdentityRead/IdentityWrite),
    /// callable by the provider-portal-bff service account. Not anonymous.
    /// </summary>
    [ApiController]
    [Route("api/v1/identity")]
    [Tags("Identity - Provider Link")]
    public sealed class ProviderLinkController : AizenWebApiController
    {
        private readonly IAizenCQRSProcessor _sender;

        public ProviderLinkController(
            IHttpContextAccessor httpContextAccessor,
            IAizenCQRSProcessor cqrsProcessor) : base(httpContextAccessor)
        {
            _sender = cqrsProcessor;
        }

        // GET /api/v1/identity/organizers/profiles/by-subject/{keycloakSubject}
        [Authorize(Policy = "IdentityRead")]
        [HttpGet("organizers/profiles/by-subject/{keycloakSubject}")]
        [ProducesResponseType(typeof(OrganizerProfileDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<AizenApiResponse<OrganizerProfileDetailDto>> GetOrganizerProfileByKeycloakSubject(
            [FromRoute] string keycloakSubject, CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(
                new GetOrganizerProfileByKeycloakSubjectQuery(keycloakSubject), ct);

            return SetResponse(result);
        }

        // POST /api/v1/identity/organizers/provision-from-keycloak
        [Authorize(Policy = "IdentityWrite")]
        [HttpPost("organizers/provision-from-keycloak")]
        [ProducesResponseType(typeof(ProvisionOrganizerFromKeycloakResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<AizenApiResponse<ProvisionOrganizerFromKeycloakResult>> ProvisionFromKeycloak(
            [FromBody] ProvisionOrganizerFromKeycloakCommand command, CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(command, ct);
            return SetResponse(result);
        }

        // POST /api/v1/identity/organizers/profiles/{profileId}/phone/mark-verified
        [Authorize(Policy = "IdentityWrite")]
        [HttpPost("organizers/profiles/{profileId:long}/phone/mark-verified")]
        [ProducesResponseType(typeof(MarkOrganizerPhoneVerifiedResult), StatusCodes.Status200OK)]
        public async Task<AizenApiResponse<MarkOrganizerPhoneVerifiedResult>> MarkPhoneVerified(
            [FromRoute] long profileId, CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(new MarkOrganizerPhoneVerifiedCommand(profileId), ct);
            return SetResponse(result);
        }
    }
}
