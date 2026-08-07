using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.InktaviaStore.Application.Identity.Query.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.InktaviaStore.Controller.V1.Identity
{
    /// <summary>
    /// BE-MO2c — general, minimal service-token lookups callable by any trusted BFF service account, following the
    /// <see cref="UserLinkController"/> convention: no blanket controller policy — each action carries its OWN
    /// explicit <c>[Authorize(Policy = "IdentityRead")]</c> (the service-token READ axis, distinct from the
    /// participant/admin user-token endpoints). These return only the minimal data a BFF needs to enrich a payload,
    /// so they are safe at IdentityRead — NOT Admin. This does not touch the Admin-heavy QueryController.
    /// </summary>
    [ApiController]
    [Route("api/v1/identity")]
    [Tags("Identity - Lookup")]
    public sealed class IdentityLookupController : AizenWebApiController
    {
        private readonly IAizenCQRSProcessor _sender;

        public IdentityLookupController(
            IHttpContextAccessor httpContextAccessor,
            IAizenCQRSProcessor cqrsProcessor) : base(httpContextAccessor)
        {
            _sender = cqrsProcessor;
        }

        /// <summary>
        /// Batch resolve profile ids → display name (CompanyName-first). Minimal projection ({ profileId,
        /// displayName }) — the reusable primitive for BFF-side provider/user name enrichment. Unknown ids are
        /// simply absent from the result. GET /api/v1/identity/profiles/display-names?ids=1&amp;ids=2…
        /// </summary>
        [Authorize(Policy = "IdentityRead")]
        [HttpGet("profiles/display-names")]
        [ProducesResponseType(typeof(List<ProfileDisplayNameDto>), StatusCodes.Status200OK)]
        public async Task<AizenApiResponse<List<ProfileDisplayNameDto>>> GetProfileDisplayNames(
            [FromQuery] long[] ids, CancellationToken ct)
        {
            var result = await _sender.ProcessAsync(new GetProfileDisplayNamesByProfileIdsQuery(ids), ct);
            return SetResponse(result?.ToList() ?? new List<ProfileDisplayNameDto>());
        }
    }
}
