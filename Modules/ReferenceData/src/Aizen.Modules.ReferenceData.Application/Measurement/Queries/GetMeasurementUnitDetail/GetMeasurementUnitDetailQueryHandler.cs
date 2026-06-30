using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Queries;

[DocumentationInfo("Returns details for a single measurement unit by ID", "Cached for 24 hours; invalidated on measurement unit changes.")]
public sealed class GetMeasurementUnitDetailQueryHandler : AizenQueryHandler<GetMeasurementUnitDetailQuery, MeasurementUnitDto?>, IAizenQueryHandlerCacheable
{
    private readonly IMeasurementReferenceService _service;

    public GetMeasurementUnitDetailQueryHandler(IMeasurementReferenceService service)
    {
        _service = service;
    }

    public override async Task<MeasurementUnitDto?> Handle(GetMeasurementUnitDetailQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetByIdAsync(request.Id, cancellationToken);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) };
}
