using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Aizen.Modules.ReferenceData.Application.Lookup.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.ReferenceData;

// ⚠️ SINIF DÜZEYİ [AllowAnonymous] — SALT-OKUNUR CONTROLLER. Buraya yazma ucu (POST/PUT/DELETE) EKLEME.
// Lookup grupları/öğeleri public referans veridir. ServiceRequest fiyatlama katmanı lookup-items'ı token'sız
// çağırır (IServiceRequestReferenceDataRemoteCall Authorization başlığı iletmez). Attribute yokken
// AddAizenKeycloakAuth'un global FallbackPolicy'si bu GET'leri de auth'a zorluyor ve modül→modül çağrıyı
// 401'e düşürüyordu. "Anonymous" = "cluster İÇİNDE token gerekmez"; modüllerin public ingress'i yoktur ve
// NetworkPolicy yalnız BFF'leri geçirir. Lookup mutasyonları ayrı, yetkili bir admin controller'ına aittir.
[ApiController]
[AllowAnonymous]
[Route("api/v1/reference-data/lookup-groups")]
[Tags("Lookup")]
[DocumentationInfo("Lookup read endpoints", "Read-only queries for lookup groups and items. Public reference data — no auth required inside the cluster.")]
public sealed class LookupController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public LookupController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LookupGroupDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IReadOnlyList<LookupGroupDto>>> GetGroups([FromQuery] bool onlyActive = true, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IReadOnlyList<LookupGroupDto>>(new GetLookupGroupListQuery(onlyActive), ct);
        return SetResponse(result);
    }

    [HttpGet("tree")]
    [ProducesResponseType(typeof(IReadOnlyList<LookupGroupTreeDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IReadOnlyList<LookupGroupTreeDto>>> GetGroupTree([FromQuery] bool onlyActive = true, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IReadOnlyList<LookupGroupTreeDto>>(new GetLookupGroupTreeQuery(onlyActive), ct);
        return SetResponse(result);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(LookupGroupDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<LookupGroupDto?>> GetGroupById([FromRoute] long id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<LookupGroupDto?>(new GetLookupGroupDetailQuery(id), ct);
        return SetResponse(result);
    }

    [HttpGet("lookup-items/{groupCode}")]
    [ProducesResponseType(typeof(IReadOnlyList<LookupItemDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IReadOnlyList<LookupItemDto>>> GetItems([FromRoute] string groupCode, [FromQuery] bool onlyActive = true, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IReadOnlyList<LookupItemDto>>(new GetLookupItemsByGroupQuery(groupCode, onlyActive), ct);
        return SetResponse(result);
    }
}
