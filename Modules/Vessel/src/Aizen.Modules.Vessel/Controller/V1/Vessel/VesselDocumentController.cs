using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Abstraction.Request.Document;
using Aizen.Modules.Vessel.Abstraction.Response.Document;
using Aizen.Modules.Vessel.Application.Command.Document;
using Aizen.Modules.Vessel.Application.Query.Document;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [ProducesResponseType(typeof(GetVesselDocumentsResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<GetVesselDocumentsResponse?>> GetAll(
        [FromRoute] long vesselId,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<GetVesselDocumentsResponse>(new GetVesselDocumentsQuery(vesselId, pageIndex, pageSize), ct);
        return SetResponse(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AddVesselDocumentResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<AddVesselDocumentResponse?>> Add([FromRoute] long vesselId, [FromBody] AddVesselDocumentRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<AddVesselDocumentResponse>(new AddVesselDocumentCommand(vesselId, req), ct);
        return SetResponse(result);
    }

    [HttpPut("{documentId:long}")]
    [ProducesResponseType(typeof(UpdateVesselDocumentResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateVesselDocumentResponse?>> Update([FromRoute] long vesselId, [FromRoute] long documentId, [FromBody] UpdateVesselDocumentRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<UpdateVesselDocumentResponse>(new UpdateVesselDocumentCommand(vesselId, documentId, req), ct);
        return SetResponse(result);
    }

    [HttpDelete("{documentId:long}")]
    [ProducesResponseType(typeof(RemoveVesselDocumentResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<RemoveVesselDocumentResponse?>> Remove([FromRoute] long vesselId, [FromRoute] long documentId, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<RemoveVesselDocumentResponse>(new RemoveVesselDocumentCommand(vesselId, documentId), ct);
        return SetResponse(result);
    }

    [HttpPatch("{documentId:long}/status")]
    [ProducesResponseType(typeof(UpdateVesselDocumentStatusResponse), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<UpdateVesselDocumentStatusResponse?>> UpdateStatus([FromRoute] long vesselId, [FromRoute] long documentId, [FromBody] VesselDocumentStatus status, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<UpdateVesselDocumentStatusResponse>(new UpdateVesselDocumentStatusCommand(vesselId, documentId, status), ct);
        return SetResponse(result);
    }
}
