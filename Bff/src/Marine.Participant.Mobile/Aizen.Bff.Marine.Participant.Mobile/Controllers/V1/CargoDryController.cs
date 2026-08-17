using Aizen.Bff.Marine.Participant.Mobile.Application.CargoDry;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Authorization;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.CargoDry;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.Marine.Participant.Mobile.Controllers.V1;

/// <summary>Owner-facing CargoDry surface (BE_MO11a): validate a scanned kit → activate it onto an owned vessel →
/// list my kits. Identity is asserted from the mobile token, never the body — the module derives the owner user id
/// from the BFF assertion, so activate and list share one id-space.</summary>
[ApiController]
[Route("api/v1/mobile/cargodry")]
[Tags("Mobile - CargoDry")]
[Authorize(Policy = ParticipantAuthorizationPolicies.ParticipantAuthenticated)]
public sealed class CargoDryController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public CargoDryController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>Validate a scanned serial/batch; on success returns a 5-minute activation token (else IsValid=false
    /// with a reason). Behind mobile auth so only signed-in owners scan.</summary>
    [HttpPost("kits/validate")]
    [ProducesResponseType(typeof(MobileKitValidationDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileKitValidationDto>> ValidateKit(
        [FromBody] MobileValidateKitRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ValidateMobileKitCommand(request.SerialNumber, request.BatchCode, request.Signature), ct);
        return SetResponse(result);
    }

    /// <summary>Activate a validated kit onto one of the caller's OWN vessels. A foreign/unknown vessel yields a clean
    /// business error and never reaches the module. Returns the activated kit.</summary>
    [HttpPost("kits/activate")]
    [ProducesResponseType(typeof(MobileKitDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileKitDto>> ActivateKit(
        [FromBody] MobileActivateKitRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new ActivateMobileKitCommand(request.ActivationToken, request.VesselId, request.Method), ct);
        return SetResponse(result);
    }

    /// <summary>The caller's kits plus roll-up counts (empty when none).</summary>
    [HttpGet("kits")]
    [ProducesResponseType(typeof(MobileMyKitsDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileMyKitsDto>> GetMyKits(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileMyKitsQuery(), ct);
        return SetResponse(result);
    }

    /// <summary>Owner-scoped detail for one of the caller's OWN kits (efficiency / expiry / renewal + vessel name +
    /// IsExpiringSoon). A foreign/unknown id yields a clean not-found; the admin detail endpoint is never exposed.</summary>
    [HttpGet("kits/{kitId:long}")]
    [ProducesResponseType(typeof(MobileKitDetailDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileKitDetailDto>> GetMyKitDetail(
        [FromRoute] long kitId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileMyKitDetailQuery(kitId), ct);
        return SetResponse(result);
    }
}
