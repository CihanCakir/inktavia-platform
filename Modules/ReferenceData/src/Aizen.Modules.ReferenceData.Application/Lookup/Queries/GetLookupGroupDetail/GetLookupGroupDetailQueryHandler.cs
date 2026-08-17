using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Queries;

[DocumentationInfo("Returns details for a single lookup group by ID", "Cached for 12 hours; invalidated on lookup group changes.")]
public sealed class GetLookupGroupDetailQueryHandler : AizenQueryHandler<GetLookupGroupDetailQuery, LookupGroupDto?>, IAizenQueryHandlerCacheable
{
    private readonly ILookupReferenceService _service;

    public GetLookupGroupDetailQueryHandler(ILookupReferenceService service)
    {
        _service = service;
    }

    public override async Task<LookupGroupDto?> Handle(GetLookupGroupDetailQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetGroupDetailAsync(request.Id, cancellationToken);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12) };
}
