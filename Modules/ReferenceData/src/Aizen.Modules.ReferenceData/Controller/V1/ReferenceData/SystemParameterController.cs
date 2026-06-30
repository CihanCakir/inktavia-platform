using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;
using Aizen.Modules.ReferenceData.Application.SystemParameter.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.ReferenceData;

[ApiController]
[Route("api/v1/reference-data/system-parameters")]
[Tags("SystemParameter")]
[DocumentationInfo("System parameter read endpoints", "Read-only queries for system parameters.")]
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
        return SetResponse(result);
    }

    [HttpGet("{key}")]
    [ProducesResponseType(typeof(SystemParameterDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SystemParameterDto?>> GetByKey([FromRoute] string key, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<SystemParameterDto?>(new GetSystemParameterByKeyQuery(key), ct);
        return SetResponse(result);
    }

    [HttpGet("by-prefix")]
    [ProducesResponseType(typeof(IReadOnlyList<SystemParameterDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IReadOnlyList<SystemParameterDto>>> GetByPrefix([FromQuery] string prefix, [FromQuery] bool onlyActive = true, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IReadOnlyList<SystemParameterDto>>(new GetSystemParametersByPrefixQuery(prefix, onlyActive), ct);
        return SetResponse(result);
    }
}
