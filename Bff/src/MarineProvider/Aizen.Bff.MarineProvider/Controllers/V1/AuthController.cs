using Aizen.Bff.MarineProvider.Application.Auth;
using Aizen.Bff.MarineProvider.Application.Auth;
using Aizen.Bff.MarineProvider.Application.Common.Authorization;
using Aizen.Bff.MarineProvider.Application.Contracts.Auth;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.MarineProvider.Controllers.V1;

[ApiController]
[Route("api/v1/provider/auth")]
[Tags("Provider - Auth")]
public sealed class AuthController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public AuthController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>Email/password provider registration (Keycloak IdP). Public.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterProviderResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RegisterProviderResponse>> Register(
        [FromBody] RegisterProviderCommand command, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(command, ct);
        return SetResponse(result);
    }

    /// <summary>Social first-login profile provisioning. Requires a valid Keycloak token.</summary>
    [Authorize(Policy = ProviderAuthorizationPolicies.ProviderAuthenticated)]
    [HttpPost("ensure-profile")]
    [ProducesResponseType(typeof(EnsureProviderProfileResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<EnsureProviderProfileResponse>> EnsureProfile(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new EnsureProviderProfileCommand(), ct);
        return SetResponse(result);
    }
}
