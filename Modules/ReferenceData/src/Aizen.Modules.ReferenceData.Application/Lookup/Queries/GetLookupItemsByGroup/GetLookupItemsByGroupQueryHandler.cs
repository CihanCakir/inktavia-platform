using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Queries;

[DocumentationInfo("Returns lookup items for a given group code", "Cached for 12 hours; invalidated on lookup item changes.")]
public sealed class GetLookupItemsByGroupQueryHandler : AizenQueryHandler<GetLookupItemsByGroupQuery, IReadOnlyList<LookupItemDto>>, IAizenQueryHandlerCacheable
{
    private readonly ILookupReferenceService _service;

    public GetLookupItemsByGroupQueryHandler(ILookupReferenceService service)
    {
        _service = service;
    }

    public override async Task<IReadOnlyList<LookupItemDto>> Handle(GetLookupItemsByGroupQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetItemsByGroupCodeAsync(request.GroupCode, request.OnlyActive, cancellationToken);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12) };
}
