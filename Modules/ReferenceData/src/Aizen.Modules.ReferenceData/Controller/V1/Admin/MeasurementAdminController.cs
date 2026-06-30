using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;
using Aizen.Modules.ReferenceData.Abstraction.Request.Measurement;
using Aizen.Modules.ReferenceData.Application.Measurement.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.Admin;

[ApiController]
[Route("api/v1/admin/reference-data/measurement-units")]
[Tags("Admin - Measurement")]
[Authorize(Roles = "Admin")]
[DocumentationInfo("Measurement unit admin endpoints", "Create, update and lifecycle management of measurement units. Requires Admin role.")]
public sealed class MeasurementAdminController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public MeasurementAdminController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpPost]
    [ProducesResponseType(typeof(MeasurementUnitDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MeasurementUnitDto?>> Create([FromBody] CreateMeasurementUnitRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<MeasurementUnitDto>(new CreateMeasurementUnitCommand(req), ct);
        return SetResponse(result);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(MeasurementUnitDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MeasurementUnitDto?>> Update([FromRoute] long id, [FromBody] UpdateMeasurementUnitRequest req, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<MeasurementUnitDto>(new UpdateMeasurementUnitCommand(id, req.Name, req.Symbol, req.ConversionFactorToBase, req.BaseUnitCode, req.IsActive), ct);
        return SetResponse(result);
    }

    [HttpPut("{id:long}/activate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> Activate([FromRoute] long id, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new ActivateMeasurementUnitCommand(id), ct);
        return SetResponse<object>(new { success = true });
    }

    [HttpPut("{id:long}/deactivate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<object>> Deactivate([FromRoute] long id, CancellationToken ct = default)
    {
        await _cqrs.ProcessAsync<bool>(new DeactivateMeasurementUnitCommand(id), ct);
        return SetResponse<object>(new { success = true });
    }
}
