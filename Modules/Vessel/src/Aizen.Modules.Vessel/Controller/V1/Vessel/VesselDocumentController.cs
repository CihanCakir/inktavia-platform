using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Vessel.Abstraction.Dto.Document;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Document;
using Aizen.Modules.Vessel.Application.Command.Document;
using Aizen.Modules.Vessel.Application.Query.Document;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Aizen.Modules.Vessel.Controller.V1.Vessel;

[ApiController]
[Route("api/v1/vessels/{vesselId:long}/documents")]
[Tags("Vessel - Documents")]
[Authorize]
[DocumentationInfo("Vessel document endpoints", "Manage vessel official documents.")]
public sealed class VesselDocumentController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public VesselDocumentController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    private long CurrentUserId =>
        long.Parse(ContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [ProducesResponseType(typeof(List<VesselDocumentDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IReadOnlyList<VesselDocumentDto>?>> GetAll([FromRoute] long vesselId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IReadOnlyList<VesselDocumentDto>>(new GetVesselDocumentsQuery(vesselId), ct);
        return SetResponse(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(VesselDocumentDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VesselDocumentDto?>> Add([FromRoute] long vesselId, [FromBody] AddVesselDocumentRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<VesselDocumentDto>(new AddVesselDocumentCommand(vesselId, req, CurrentUserId), ct);
        return SetResponse(result);
    }

    [HttpPut("{documentId:long}")]
    [ProducesResponseType(typeof(VesselDocumentDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VesselDocumentDto?>> Update([FromRoute] long vesselId, [FromRoute] long documentId, [FromBody] UpdateVesselDocumentRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<VesselDocumentDto>(new UpdateVesselDocumentCommand(vesselId, documentId, req, CurrentUserId), ct);
        return SetResponse(result);
    }

    [HttpDelete("{documentId:long}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> Remove([FromRoute] long vesselId, [FromRoute] long documentId, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new RemoveVesselDocumentCommand(vesselId, documentId, CurrentUserId), ct);
        return SetResponse<object>(new { success = true });
    }

    [HttpPatch("{documentId:long}/status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> UpdateStatus([FromRoute] long vesselId, [FromRoute] long documentId, [FromBody] VesselDocumentStatus status, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new UpdateVesselDocumentStatusCommand(vesselId, documentId, status, CurrentUserId), ct);
        return SetResponse<object>(new { success = true });
    }
}
