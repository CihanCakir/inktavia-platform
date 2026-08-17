using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;
using Aizen.Modules.ReferenceData.Application.SystemParameter.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.ReferenceData;

// ⚠️ CLASS-LEVEL [AllowAnonymous] — READ-ONLY CONTROLLER. Do not add a write endpoint here.
// System parameters are cluster-internal reference config (fee/VAT rates, default currency/language, feature
// flags). Other modules read them service-to-service (e.g. payment-api's economics resolves PLATFORM_FEE_VAT_RATE
// / COMMISSION_VAT_RATE via IPaymentReferenceDataRemoteCall); those calls carry no user token, matching the
// sibling read controllers (Measurement, Location). Mutations belong on the admin controller, never here.
// "Anonymous" means "no token required inside the cluster", not "exposed to the internet": modules have no public
// ingress and a NetworkPolicy admits only the BFFs (infrastructure/k8s). That boundary is what makes this safe.
// SECURITY: encrypted parameter values are NEVER exposed on this anonymous surface — MaskEncrypted() nulls the
// Value of any IsEncrypted parameter (the flag is kept so callers know a value exists but is withheld). Consumers
// that legitimately need a decrypted value must read it in-process, not through this endpoint.
[ApiController]
[Route("api/v1/reference-data/system-parameters")]
[Tags("SystemParameter")]
[AllowAnonymous]
[DocumentationInfo("System parameter read endpoints", "Read-only queries for system parameters. Cluster-internal reference config — no auth required. Encrypted values are masked. Read-only: do not add write endpoints under this class-level [AllowAnonymous].")]
public sealed class SystemParameterController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public SystemParameterController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SystemParameterDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IReadOnlyList<SystemParameterDto>>> GetList([FromQuery] bool onlyActive = true, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IReadOnlyList<SystemParameterDto>>(new GetSystemParameterListQuery(onlyActive), ct);
        return SetResponse(MaskEncrypted(result));
    }

    [HttpGet("{key}")]
    [ProducesResponseType(typeof(SystemParameterDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SystemParameterDto?>> GetByKey([FromRoute] string key, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<SystemParameterDto?>(new GetSystemParameterByKeyQuery(key), ct);
        return SetResponse(MaskEncrypted(result));
    }

    [HttpGet("by-prefix")]
    [ProducesResponseType(typeof(IReadOnlyList<SystemParameterDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IReadOnlyList<SystemParameterDto>>> GetByPrefix([FromQuery] string prefix, [FromQuery] bool onlyActive = true, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IReadOnlyList<SystemParameterDto>>(new GetSystemParametersByPrefixQuery(prefix, onlyActive), ct);
        return SetResponse(MaskEncrypted(result));
    }

    // ── Encrypted-value redaction for the anonymous read surface ──────────────────────────────────────────────
    // Never emit the Value of an IsEncrypted parameter. IsEncrypted stays true so a caller can tell the parameter
    // exists but its value is withheld; economics reads only non-encrypted params, so this never affects it.
    private static SystemParameterDto? MaskEncrypted(SystemParameterDto? dto)
    {
        if (dto is { IsEncrypted: true })
            dto.Value = null!;
        return dto;
    }

    private static IReadOnlyList<SystemParameterDto> MaskEncrypted(IReadOnlyList<SystemParameterDto> dtos)
    {
        foreach (var dto in dtos)
            MaskEncrypted(dto);
        return dtos;
    }
}
