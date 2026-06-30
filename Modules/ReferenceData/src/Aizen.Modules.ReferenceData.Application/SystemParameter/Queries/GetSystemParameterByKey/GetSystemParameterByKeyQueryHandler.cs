using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;

namespace Aizen.Modules.ReferenceData.Application.SystemParameter.Queries;

[DocumentationInfo("Returns a system parameter by key", "Cached for 60 minutes; invalidated on system parameter changes.")]
public sealed class GetSystemParameterByKeyQueryHandler : AizenQueryHandler<GetSystemParameterByKeyQuery, SystemParameterDto?>, IAizenQueryHandlerCacheable
{
    private readonly ISystemParameterReferenceService _service;

    public GetSystemParameterByKeyQueryHandler(ISystemParameterReferenceService service)
    {
        _service = service;
    }

    public override async Task<SystemParameterDto?> Handle(GetSystemParameterByKeyQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetByKeyAsync(request.Key, cancellationToken);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60) };
}
