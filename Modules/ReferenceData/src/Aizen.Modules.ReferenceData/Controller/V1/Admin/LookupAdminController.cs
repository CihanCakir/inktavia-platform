using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupItem;
using Aizen.Modules.ReferenceData.Application.Lookup.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[DocumentationInfo("Lookup admin endpoints", "Create, update and lifecycle management of lookup groups and items. Requires Admin role.")]
public sealed class LookupAdminController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public LookupAdminController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    // ===== Lookup Groups =====

    [HttpPost("api/v1/admin/reference-data/lookup-groups")]
    [Tags("Admin - Lookup")]
    [ProducesResponseType(typeof(LookupGroupDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<LookupGroupDto?>> CreateGroup([FromBody] CreateLookupGroupRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<LookupGroupDto>(new CreateLookupGroupCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPut("api/v1/admin/reference-data/lookup-groups/{id:long}")]
    [Tags("Admin - Lookup")]
    [ProducesResponseType(typeof(LookupGroupDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<LookupGroupDto?>> UpdateGroup([FromRoute] long id, [FromBody] UpdateLookupGroupRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<LookupGroupDto>(new UpdateLookupGroupCommand(id, req), ct);
        return SetResponse(result);
    }

    [HttpPut("api/v1/admin/reference-data/lookup-groups/move")]
    [Tags("Admin - Lookup")]
    [ProducesResponseType(typeof(LookupGroupTreeDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<LookupGroupTreeDto?>> MoveGroup([FromBody] MoveLookupGroupRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<LookupGroupTreeDto>(new MoveLookupGroupCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPut("api/v1/admin/reference-data/lookup-groups/{id:long}/activate")]
    [Tags("Admin - Lookup")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> ActivateGroup([FromRoute] long id, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new ActivateLookupGroupCommand(id), ct);
        return SetResponse<object>(new { success = true });
    }

    [HttpPut("api/v1/admin/reference-data/lookup-groups/{id:long}/deactivate")]
    [Tags("Admin - Lookup")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> DeactivateGroup([FromRoute] long id, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new DeactivateLookupGroupCommand(id), ct);
        return SetResponse<object>(new { success = true });
    }

    // ===== Lookup Items =====

    [HttpPost("api/v1/admin/reference-data/lookup-items")]
    [Tags("Admin - Lookup")]
    [ProducesResponseType(typeof(LookupItemDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<LookupItemDto?>> CreateItem([FromBody] CreateLookupItemRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<LookupItemDto>(new CreateLookupItemCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPut("api/v1/admin/reference-data/lookup-items/{id:long}")]
    [Tags("Admin - Lookup")]
    [ProducesResponseType(typeof(LookupItemDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<LookupItemDto?>> UpdateItem([FromRoute] long id, [FromBody] UpdateLookupItemRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<LookupItemDto>(new UpdateLookupItemCommand(id, req.Name, req.Description, req.IconKey, req.ColorCode, req.SortOrder, req.IsDefault, req.IsActive, req.DisplayNameTr), ct);
        return SetResponse(result);
    }

    [HttpPut("api/v1/admin/reference-data/lookup-items/{id:long}/activate")]
    [Tags("Admin - Lookup")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> ActivateItem([FromRoute] long id, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new ActivateLookupItemCommand(id), ct);
        return SetResponse<object>(new { success = true });
    }

    [HttpPut("api/v1/admin/reference-data/lookup-items/{id:long}/deactivate")]
    [Tags("Admin - Lookup")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> DeactivateItem([FromRoute] long id, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new DeactivateLookupItemCommand(id), ct);
        return SetResponse<object>(new { success = true });
    }
}
