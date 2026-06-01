using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Measurement;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Model;

namespace Aizen.Modules.ReferenceData.Application.Measurement.Queries;

[DocumentationInfo("Returns the list of all measurement units", "Cached for 24 hours; invalidated on measurement unit changes.")]
public sealed class GetMeasurementUnitListQueryHandler : AizenQueryHandler<GetMeasurementUnitListQuery, IReadOnlyList<MeasurementUnitDto>>, IAizenQueryHandlerCacheable
{
    private readonly IMeasurementReferenceService _service;

    public GetMeasurementUnitListQueryHandler(IMeasurementReferenceService service)
    {
        _service = service;
    }

    public override async Task<IReadOnlyList<MeasurementUnitDto>> Handle(GetMeasurementUnitListQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetListAsync(request.OnlyActive, cancellationToken);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) };
}
