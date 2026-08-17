using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Queries;

[DocumentationInfo("Returns the list of all lookup groups", "Cached for 12 hours; invalidated on lookup group changes.")]
public sealed class GetLookupGroupListQueryHandler : AizenQueryHandler<GetLookupGroupListQuery, IReadOnlyList<LookupGroupDto>>, IAizenQueryHandlerCacheable
{
    private readonly ILookupReferenceService _service;

    public GetLookupGroupListQueryHandler(ILookupReferenceService service)
    {
        _service = service;
    }

    public override async Task<IReadOnlyList<LookupGroupDto>> Handle(GetLookupGroupListQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetGroupsAsync(request.OnlyActive, cancellationToken);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12) };
}
