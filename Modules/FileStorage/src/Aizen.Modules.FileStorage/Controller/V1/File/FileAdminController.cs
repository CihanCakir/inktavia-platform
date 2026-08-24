using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Aizen.Modules.FileStorage.Application.Queries.GetAdminFileList;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.FileStorage.Controller.V1.File;

// Admin-scoped dosya listeleme. Ayrı controller: yalnızca "Admin" rolüne açık ve
// mevcut FileController'ın [Authorize] (herhangi kimlik) sözleşmesini kirletmez.
[ApiController]
[Route("api/v1/file/admin")]
[Tags("File - Admin")]
[Authorize(Roles = "Admin")]
[DocumentationInfo("File admin controller", "Admin-scoped file listing endpoints for the FileStorage module.")]
public sealed class FileAdminController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public FileAdminController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet("files")]
    [ProducesResponseType(typeof(AdminFileListResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AdminFileListResult>> GetFiles(
        [FromQuery] string? search,
        [FromQuery] string? contentType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var request = new AdminFileListRequest
        {
            Search = search,
            ContentType = contentType,
            Page = page,
            PageSize = pageSize
        };

        var result = await _cqrs.ProcessAsync<AdminFileListResult>(new GetAdminFileListQuery(request), ct);
        return SetResponse(result);
    }
}
