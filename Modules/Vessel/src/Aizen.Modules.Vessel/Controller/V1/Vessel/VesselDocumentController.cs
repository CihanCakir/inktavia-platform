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
using MiniUow.Paging;

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

    [HttpGet]
    [ProducesResponseType(typeof(IPaginate<VesselDocumentDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IPaginate<VesselDocumentDto>?>> GetAll(
        [FromRoute] long vesselId,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IPaginate<VesselDocumentDto>>(new GetVesselDocumentsQuery(vesselId, pageIndex, pageSize), ct);
        return SetResponse(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(VesselDocumentDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VesselDocumentDto?>> Add([FromRoute] long vesselId, [FromBody] AddVesselDocumentRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<VesselDocumentDto>(new AddVesselDocumentCommand(vesselId, req), ct);
        return SetResponse(result);
    }

    [HttpPut("{documentId:long}")]
    [ProducesResponseType(typeof(VesselDocumentDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<VesselDocumentDto?>> Update([FromRoute] long vesselId, [FromRoute] long documentId, [FromBody] UpdateVesselDocumentRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<VesselDocumentDto>(new UpdateVesselDocumentCommand(vesselId, documentId, req), ct);
        return SetResponse(result);
    }

    [HttpDelete("{documentId:long}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> Remove([FromRoute] long vesselId, [FromRoute] long documentId, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new RemoveVesselDocumentCommand(vesselId, documentId), ct);
        return SetResponse<object>(new { success = true });
    }

    [HttpPatch("{documentId:long}/status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> UpdateStatus([FromRoute] long vesselId, [FromRoute] long documentId, [FromBody] VesselDocumentStatus status, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new UpdateVesselDocumentStatusCommand(vesselId, documentId, status), ct);
        return SetResponse<object>(new { success = true });
    }
}
