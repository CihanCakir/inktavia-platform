using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;   // BoolResult
using Aizen.Modules.ReferenceData.Abstraction.Dto.Marina;
using Aizen.Modules.ReferenceData.Abstraction.Request.Marina;
using Aizen.Modules.ReferenceData.Application.Marina;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.Admin;

/// <summary>Admin curation for the marina reference catalog (list / edit / mark-reviewed / deactivate). Admin-role only.</summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/reference-data/marinas")]
[Tags("Admin - Marinas")]
public sealed class MarinaAdminController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    public MarinaAdminController(IHttpContextAccessor http, IAizenCQRSProcessor cqrs) : base(http) => _cqrs = cqrs;

    [HttpGet]
    public async Task<AizenApiResponse<MarinaAdminListResult>> List(
        [FromQuery] bool needsReview = false, [FromQuery] string? search = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<MarinaAdminListResult>(
            new ListMarinasForAdminQuery(needsReview, search, page, pageSize), ct));

    [HttpPut("{id:long}")]
    public async Task<AizenApiResponse<BoolResult?>> Update([FromRoute] long id, [FromBody] UpdateMarinaRequest req, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<BoolResult>(new UpdateMarinaCommand(id, req.Name, req.CityCode), ct));

    [HttpPut("{id:long}/mark-reviewed")]
    public async Task<AizenApiResponse<BoolResult?>> MarkReviewed([FromRoute] long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<BoolResult>(new MarkMarinaReviewedCommand(id), ct));

    [HttpPut("{id:long}/deactivate")]
    public async Task<AizenApiResponse<BoolResult?>> Deactivate([FromRoute] long id, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<BoolResult>(new DeactivateMarinaCommand(id), ct));
}
