using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;
using Aizen.Modules.Vessel.Application.Query.Vessel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Vessel.Controller.V1.Admin.Vessel;

[ApiController]
[Route("api/v1/admin/vessels")]
[Tags("Admin - Vessel")]
[Authorize(Roles = RoleNames.Admin)]
[DocumentationInfo("Admin vessel endpoints", "Admin-only vessel management and reporting.")]
public sealed class VesselAdminController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public VesselAdminController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetAllVesselsAdminResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetAllVesselsAdminResponse?>> GetAll(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isArchived = null,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetAllVesselsAdminResponse>(
            new GetAllVesselsAdminQuery(pageIndex, pageSize, searchTerm, isArchived), ct);
        return SetResponse(result);
    }
}
