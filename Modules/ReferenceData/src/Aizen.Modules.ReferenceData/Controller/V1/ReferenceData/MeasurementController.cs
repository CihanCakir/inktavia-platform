using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;
using Aizen.Modules.ReferenceData.Abstraction.Enum;
using Aizen.Modules.ReferenceData.Abstraction.Model;
using Aizen.Modules.ReferenceData.Abstraction.Request.Measurement;
using Aizen.Modules.ReferenceData.Application.Measurement.Commands;
using Aizen.Modules.ReferenceData.Application.Measurement.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.ReferenceData;

[ApiController]
[Route("api/v1/reference-data/measurement-units")]
[Tags("Measurement")]
[DocumentationInfo("Measurement unit management endpoints", "CRUD and lifecycle operations for measurement units.")]
public sealed class MeasurementController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public MeasurementController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MeasurementUnitDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IReadOnlyList<MeasurementUnitDto>>> GetList([FromQuery] bool onlyActive = true, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IReadOnlyList<MeasurementUnitDto>>(new GetMeasurementUnitListQuery(onlyActive), ct);
        return SetResponse(result);
    }

    [HttpGet("by-type/{unitType}")]
    [ProducesResponseType(typeof(IReadOnlyList<MeasurementUnitDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<IReadOnlyList<MeasurementUnitDto>>> GetByType([FromRoute] MeasurementUnitType unitType, [FromQuery] bool onlyActive = true, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<IReadOnlyList<MeasurementUnitDto>>(new GetMeasurementUnitsByTypeQuery(unitType, onlyActive), ct);
        return SetResponse(result);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(MeasurementUnitDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MeasurementUnitDto?>> GetById([FromRoute] long id, CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<MeasurementUnitDto?>(new GetMeasurementUnitDetailQuery(id), ct);
        return SetResponse(result);
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
