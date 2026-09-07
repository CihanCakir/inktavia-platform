using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Catalog;   // BoolResult
using Aizen.Modules.ReferenceData.Abstraction.Dto.Marina;
using Aizen.Modules.ReferenceData.Abstraction.Request.Marina;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.AdminPanel.Controllers.V1;

/// <summary>Admin marina curation. Thin passthrough to the ReferenceData module admin endpoints
/// (the admin service token carries the Admin role the module authorizes on).</summary>
[ApiController]
[Route("api/v1/admin-panel/reference-data/marinas")]
[Tags("Admin Panel - Marinas")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class MarinaAdminController : AizenWebApiController
{
    private readonly IReferenceDataRemoteCall _rd;
    public MarinaAdminController(IHttpContextAccessor http, IReferenceDataRemoteCall rd) : base(http) => _rd = rd;

    [HttpGet]
    public async Task<AizenApiResponse<MarinaAdminListResult?>> List(
        [FromQuery] bool needsReview = false, [FromQuery] string? search = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => SetResponse((await _rd.GetMarinasForAdmin(needsReview, search, page, pageSize))?.Body);

    [HttpPut("{id:long}")]
    public async Task<AizenApiResponse<BoolResult?>> Update(long id, [FromBody] UpdateMarinaRequest req)
        => SetResponse((await _rd.UpdateMarina(id, req))?.Body);

    // admin-web PUTs .../{id}/reviewed → module mark-reviewed.
    [HttpPut("{id:long}/reviewed")]
    public async Task<AizenApiResponse<BoolResult?>> MarkReviewed(long id)
        => SetResponse((await _rd.MarkMarinaReviewed(id))?.Body);

    [HttpPut("{id:long}/deactivate")]
    public async Task<AizenApiResponse<BoolResult?>> Deactivate(long id)
        => SetResponse((await _rd.DeactivateMarina(id))?.Body);
}
