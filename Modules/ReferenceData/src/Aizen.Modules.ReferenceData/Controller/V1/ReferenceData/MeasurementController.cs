using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;
using Aizen.Modules.ReferenceData.Abstraction.Enum;
using Aizen.Modules.ReferenceData.Application.Measurement.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.ReferenceData.Controller.V1.ReferenceData;

// ⚠️ CLASS-LEVEL [AllowAnonymous] — READ-ONLY CONTROLLER. Do not add a write endpoint here.
// Measurement units are public reference data (the offer builder validates unit codes against them). These
// GETs carry no token. [AllowAnonymous] is on the CLASS: any action added below inherits it — a write
// endpoint dropped in here would be publicly writable. Mutations belong on a separate admin controller.
// "Anonymous" means "no token required inside the cluster", not "exposed to the internet": modules have no
// public ingress and a NetworkPolicy admits only the BFFs (infrastructure/k8s). That boundary is what makes
// this safe — if it is removed, this endpoint is genuinely open.
[ApiController]
[Route("api/v1/reference-data/measurement-units")]
[Tags("Measurement")]
[AllowAnonymous]
[DocumentationInfo("Measurement unit read endpoints", "Read-only queries for measurement units. Public reference data — no auth required. Read-only: do not add write endpoints under this class-level [AllowAnonymous].")]
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
