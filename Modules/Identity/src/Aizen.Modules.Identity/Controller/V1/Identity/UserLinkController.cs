using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.InktaviaStore.Application.Identity.Query.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.InktaviaStore.Controller.V1.Identity
{
    /// <summary>
    /// Generic Keycloak ↔ Identity user-link endpoints. Service-token authorized (IdentityRead), callable by any
    /// trusted BFF service account (e.g. admin-panel-bff). A mirror of the organizer by-subject lookup on
    /// <see cref="ProviderLinkController"/>, but for the plain user id — used by BFFs to resolve the acting user's
    /// numeric id from the Keycloak subject for the identity assertion. Not anonymous. Does not touch organizer/
    /// venue/participant paths.
    /// </summary>
    [ApiController]
    [Route("api/v1/identity")]
    [Tags("Identity - User Link")]
    public sealed class UserLinkController : AizenWebApiController
    {
        private readonly IAizenCQRSProcessor _sender;

        public UserLinkController(
            IHttpContextAccessor httpContextAccessor,
            IAizenCQRSProcessor cqrsProcessor) : base(httpContextAccessor)
        {
            _sender = cqrsProcessor;
        }

        // GET /api/v1/identity/users/by-subject/{keycloakSubject}
        [Authorize(Policy = "IdentityRead")]
        [HttpGet("users/by-subject/{keycloakSubject}")]
        [ProducesResponseType(typeof(UserBySubjectDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<AizenApiResponse<UserBySubjectDto>> GetUserByKeycloakSubject(
            [FromRoute] string keycloakSubject, CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(
                new GetUserByKeycloakSubjectQuery(keycloakSubject), ct);

            return SetResponse(result);
        }
    }
}
