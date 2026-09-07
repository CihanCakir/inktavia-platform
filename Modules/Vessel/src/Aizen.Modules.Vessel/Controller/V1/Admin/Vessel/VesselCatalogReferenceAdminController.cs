using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Dto.CatalogReference;
using Aizen.Modules.Vessel.Abstraction.Request.CatalogReference;
using Aizen.Modules.Vessel.Application.CatalogReference;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Vessel.Controller.V1.Admin.Vessel;

/// <summary>Admin-only view + mutation of the Vessel-owned catalog reference columns: grouped counts (for the
/// catalog review screen) and the merge repoint (move all vessel references from a source catalog id to a target).</summary>
[ApiController]
[Route("api/v1/admin/vessels/catalog-references")]
[Tags("Admin - Vessel Catalog References")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class VesselCatalogReferenceAdminController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    public VesselCatalogReferenceAdminController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor) => _cqrs = cqrs;

    [HttpGet("counts")]
    public async Task<AizenApiResponse<CatalogReferenceCountsDto?>> Counts(CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<CatalogReferenceCountsDto>(new GetVesselCatalogReferenceCountsQuery(), ct));

    [HttpPost("repoint")]
    public async Task<AizenApiResponse<CatalogRepointResultDto?>> Repoint([FromBody] RepointCatalogReferenceRequest req, CancellationToken ct = default)
        => SetResponse(await _cqrs.ProcessAsync<CatalogRepointResultDto>(
            new RepointVesselCatalogReferenceCommand(req.Kind, req.SourceId, req.TargetId), ct));
}
