using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Model;

namespace Aizen.Modules.ReferenceData.Application.SystemParameter.Queries;

[DocumentationInfo("Returns system parameters matching a key prefix", "Cached for 60 minutes; invalidated on system parameter changes.")]
public sealed class GetSystemParametersByPrefixQueryHandler : AizenQueryHandler<GetSystemParametersByPrefixQuery, IReadOnlyList<SystemParameterDto>>, IAizenQueryHandlerCacheable
{
    private readonly ISystemParameterReferenceService _service;

    public GetSystemParametersByPrefixQueryHandler(ISystemParameterReferenceService service)
    {
        _service = service;
    }

    public override async Task<IReadOnlyList<SystemParameterDto>> Handle(GetSystemParametersByPrefixQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetByPrefixAsync(request.Prefix, request.OnlyActive, cancellationToken);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60) };
}
