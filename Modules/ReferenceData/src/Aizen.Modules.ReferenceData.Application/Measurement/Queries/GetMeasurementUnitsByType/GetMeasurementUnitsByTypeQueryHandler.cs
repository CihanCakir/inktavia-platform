using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Queries;

[DocumentationInfo("Returns measurement units filtered by type", "Cached for 24 hours; invalidated on measurement unit changes.")]
public sealed class GetMeasurementUnitsByTypeQueryHandler : AizenQueryHandler<GetMeasurementUnitsByTypeQuery, IReadOnlyList<MeasurementUnitDto>>, IAizenQueryHandlerCacheable
{
    private readonly IMeasurementReferenceService _service;

    public GetMeasurementUnitsByTypeQueryHandler(IMeasurementReferenceService service)
    {
        _service = service;
    }

    public override async Task<IReadOnlyList<MeasurementUnitDto>> Handle(GetMeasurementUnitsByTypeQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetByTypeAsync(request.UnitType, request.OnlyActive, cancellationToken);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) };
}
