using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;
using Aizen.Modules.ReferenceData.Abstraction.Request.SystemParameter;
using Aizen.Modules.ReferenceData.Application.SystemParameter.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.Admin;

[ApiController]
[Route("api/v1/admin/reference-data/system-parameters")]
[Tags("Admin - SystemParameter")]
[Authorize(Roles = "Admin")]
[DocumentationInfo("System parameter admin endpoints", "Create, update and lifecycle management of system parameters. Requires Admin role.")]
public sealed class SystemParameterAdminController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public SystemParameterAdminController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
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
