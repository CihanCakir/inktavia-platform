using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;
using Aizen.Modules.ReferenceData.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.Admin;

/// <summary>Catalog-side of a duplicate merge (validate + deactivate source with audit). The Vessel FK repoint is
/// orchestrated by the BFF against the Vessel module — see the merge report. Admin-role only.</summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/reference-data/catalog")]
[Tags("Admin - Catalog Merge")]
public sealed class CatalogMergeAdminController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    public CatalogMergeAdminController(IHttpContextAccessor http, IAizenCQRSProcessor cqrs) : base(http) => _cqrs = cqrs;

    [HttpPost("merge")]
    public async Task<AizenApiResponse<CatalogMergeResultDto?>> Merge([FromBody] MergeCatalogRequest req, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<CatalogMergeResultDto>(
            new MergeCatalogEntryCommand(req.Type, req.SourceId, req.TargetId), ct));
}
