using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Queries;

[DocumentationInfo("Returns a single measurement unit by code (R3)", "Cached for 24 hours; measurement units are near-static reference data.")]
public sealed class GetMeasurementUnitByCodeQueryHandler : AizenQueryHandler<GetMeasurementUnitByCodeQuery, MeasurementUnitDto?>, IAizenQueryHandlerCacheable
{
    private readonly IMeasurementReferenceService _service;

    public GetMeasurementUnitByCodeQueryHandler(IMeasurementReferenceService service)
    {
        _service = service;
    }

    public override async Task<MeasurementUnitDto?> Handle(GetMeasurementUnitByCodeQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetByCodeAsync(request.Code, cancellationToken);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) };
}
