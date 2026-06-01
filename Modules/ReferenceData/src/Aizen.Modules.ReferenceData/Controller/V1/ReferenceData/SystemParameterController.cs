using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;
using Aizen.Modules.ReferenceData.Abstraction.Model;
using Aizen.Modules.ReferenceData.Abstraction.Request.SystemParameter;
using Aizen.Modules.ReferenceData.Application.SystemParameter.Commands;
using Aizen.Modules.ReferenceData.Application.SystemParameter.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.ReferenceData;

[ApiController]
[Route("api/v1/reference-data/system-parameters")]
[Tags("SystemParameter")]
[DocumentationInfo("System parameter management endpoints", "CRUD and lifecycle operations for system parameters.")]
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

    [HttpPost]
    [ProducesResponseType(typeof(SystemParameterDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SystemParameterDto?>> Create([FromBody] CreateSystemParameterRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<SystemParameterDto>(new CreateSystemParameterCommand(req.Key, req.Value, req.ValueType, req.Description, req.IsEncrypted), ct);
        return SetResponse(result);
    }

    [HttpPut("{key}")]
    [ProducesResponseType(typeof(SystemParameterDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SystemParameterDto?>> Update([FromRoute] string key, [FromBody] UpdateSystemParameterRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<SystemParameterDto>(new UpdateSystemParameterCommand(key, req.Value, req.Description, req.IsActive), ct);
        return SetResponse(result);
    }

    [HttpPut("{key}/activate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> Activate([FromRoute] string key, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new ActivateSystemParameterCommand(key), ct);
        return SetResponse<object>(new { success = true });
    }

    [HttpPut("{key}/deactivate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> Deactivate([FromRoute] string key, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new DeactivateSystemParameterCommand(key), ct);
        return SetResponse<object>(new { success = true });
    }
}
