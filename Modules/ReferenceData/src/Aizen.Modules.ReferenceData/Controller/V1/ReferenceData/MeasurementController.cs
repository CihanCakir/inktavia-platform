using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;
using Aizen.Modules.ReferenceData.Abstraction.Enum;
using Aizen.Modules.ReferenceData.Application.Measurement.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.ReferenceData;

[ApiController]
[Route("api/v1/reference-data/measurement-units")]
[Tags("Measurement")]
[DocumentationInfo("Measurement unit read endpoints", "Read-only queries for measurement units.")]
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
}
