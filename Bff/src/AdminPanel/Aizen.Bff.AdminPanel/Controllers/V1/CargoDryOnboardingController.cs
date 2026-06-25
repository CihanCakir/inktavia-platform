using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

[ApiController]
[Route("api/v1/onboarding/cargodry")]
[Tags("Onboarding - CargoDry")]
public sealed class CargoDryOnboardingController : AizenWebApiController
{
    private readonly IAdminCargoDryBffRemoteCall _cargoDry;

    public CargoDryOnboardingController(
        IHttpContextAccessor httpContextAccessor,
        IAdminCargoDryBffRemoteCall cargoDry)
        : base(httpContextAccessor)
    {
        _cargoDry = cargoDry;
    }

    /// <summary>
    /// POST /api/v1/onboarding/cargodry/validate
    /// [AllowAnonymous] — user can scan QR before logging in.
    /// Returns a 5-minute ActivationToken; frontend saves it in sessionStorage,
    /// sends it to /activate after completing login.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("validate")]
    [ProducesResponseType(typeof(CargoDryValidationBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryValidationBffDto>> Validate(
        [FromBody] ValidateKitBffRequest request, CancellationToken ct)
    {
        var result = await _cargoDry.ValidateKitAsync(request, ct);
        return SetResponse(result);
    }

    /// <summary>
    /// POST /api/v1/onboarding/cargodry/activate
    /// [Authorize] — user must be logged in to complete activation.
    /// Body: { activationToken (5-min JWT), vesselId }
    /// </summary>
    [Authorize]
    [HttpPost("activate")]
    [ProducesResponseType(typeof(CargoDryKitBffDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CargoDryKitBffDto>> Activate(
        [FromBody] ActivateKitBffRequest request, CancellationToken ct)
    {
        var result = await _cargoDry.ActivateKitAsync(request, ct);
        return SetResponse(result);
    }
}
