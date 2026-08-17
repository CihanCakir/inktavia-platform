using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Queries;

[DocumentationInfo("Returns the hierarchical lookup group tree", "Cached for 12 hours; invalidated on lookup group changes.")]
public sealed class GetLookupGroupTreeQueryHandler : AizenQueryHandler<GetLookupGroupTreeQuery, IReadOnlyList<LookupGroupTreeDto>>, IAizenQueryHandlerCacheable
{
    private readonly ILookupTreeService _service;

    public GetLookupGroupTreeQueryHandler(ILookupTreeService service)
    {
        _service = service;
    }

    public override async Task<IReadOnlyList<LookupGroupTreeDto>> Handle(GetLookupGroupTreeQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetTreeAsync(request.OnlyActive, cancellationToken);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12) };
}
