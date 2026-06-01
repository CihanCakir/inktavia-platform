using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Aizen.Modules.ReferenceData.Abstraction.Model;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupItem;
using Aizen.Modules.ReferenceData.Application.Lookup.Commands;
using Aizen.Modules.ReferenceData.Application.Lookup.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.ReferenceData;

[ApiController]
[Tags("Lookup")]
[DocumentationInfo("Lookup group and item management endpoints", "CRUD and lifecycle operations for lookup groups and items.")]
public sealed class LookupController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public LookupController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    // ===== Lookup Groups =====

    [HttpGet("api/v1/reference-data/lookup-groups")]
    [ProducesResponseType(typeof(IReadOnlyList<LookupGroupDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IReadOnlyList<LookupGroupDto>>> GetGroups([FromQuery] bool onlyActive = true, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IReadOnlyList<LookupGroupDto>>(new GetLookupGroupListQuery(onlyActive), ct);
        return SetResponse(result);
    }

    [HttpGet("api/v1/reference-data/lookup-groups/tree")]
    [ProducesResponseType(typeof(IReadOnlyList<LookupGroupTreeDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IReadOnlyList<LookupGroupTreeDto>>> GetGroupTree([FromQuery] bool onlyActive = true, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IReadOnlyList<LookupGroupTreeDto>>(new GetLookupGroupTreeQuery(onlyActive), ct);
        return SetResponse(result);
    }

    [HttpGet("api/v1/reference-data/lookup-groups/{id:long}")]
    [ProducesResponseType(typeof(LookupGroupDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<LookupGroupDto?>> GetGroupById([FromRoute] long id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<LookupGroupDto?>(new GetLookupGroupDetailQuery(id), ct);
        return SetResponse(result);
    }

    [HttpPost("api/v1/reference-data/lookup-groups")]
    [ProducesResponseType(typeof(LookupGroupDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<LookupGroupDto?>> CreateGroup([FromBody] CreateLookupGroupRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<LookupGroupDto>(new CreateLookupGroupCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPut("api/v1/reference-data/lookup-groups/{id:long}")]
    [ProducesResponseType(typeof(LookupGroupDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<LookupGroupDto?>> UpdateGroup([FromRoute] long id, [FromBody] UpdateLookupGroupRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<LookupGroupDto>(new UpdateLookupGroupCommand(id, req), ct);
        return SetResponse(result);
    }

    [HttpPut("api/v1/reference-data/lookup-groups/move")]
    [ProducesResponseType(typeof(LookupGroupTreeDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<LookupGroupTreeDto?>> MoveGroup([FromBody] MoveLookupGroupRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<LookupGroupTreeDto>(new MoveLookupGroupCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPut("api/v1/reference-data/lookup-groups/{id:long}/activate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> ActivateGroup([FromRoute] long id, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new ActivateLookupGroupCommand(id), ct);
        return SetResponse<object>(new { success = true });
    }

    [HttpPut("api/v1/reference-data/lookup-groups/{id:long}/deactivate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> DeactivateGroup([FromRoute] long id, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new DeactivateLookupGroupCommand(id), ct);
        return SetResponse<object>(new { success = true });
    }

    // ===== Lookup Items =====

    [HttpGet("api/v1/reference-data/lookup-items/{groupCode}")]
    [ProducesResponseType(typeof(IReadOnlyList<LookupItemDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IReadOnlyList<LookupItemDto>>> GetItems([FromRoute] string groupCode, [FromQuery] bool onlyActive = true, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IReadOnlyList<LookupItemDto>>(new GetLookupItemsByGroupQuery(groupCode, onlyActive), ct);
        return SetResponse(result);
    }

    [HttpPost("api/v1/reference-data/lookup-items")]
    [ProducesResponseType(typeof(LookupItemDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<LookupItemDto?>> CreateItem([FromBody] CreateLookupItemRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<LookupItemDto>(new CreateLookupItemCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPut("api/v1/reference-data/lookup-items/{id:long}")]
    [ProducesResponseType(typeof(LookupItemDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<LookupItemDto?>> UpdateItem([FromRoute] long id, [FromBody] UpdateLookupItemRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<LookupItemDto>(new UpdateLookupItemCommand(id, req.Name, req.Description, req.IconKey, req.ColorCode, req.SortOrder, req.IsDefault, req.IsActive), ct);
        return SetResponse(result);
    }

    [HttpPut("api/v1/reference-data/lookup-items/{id:long}/activate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> ActivateItem([FromRoute] long id, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new ActivateLookupItemCommand(id), ct);
        return SetResponse<object>(new { success = true });
    }

    [HttpPut("api/v1/reference-data/lookup-items/{id:long}/deactivate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> DeactivateItem([FromRoute] long id, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new DeactivateLookupItemCommand(id), ct);
        return SetResponse<object>(new { success = true });
    }
}
