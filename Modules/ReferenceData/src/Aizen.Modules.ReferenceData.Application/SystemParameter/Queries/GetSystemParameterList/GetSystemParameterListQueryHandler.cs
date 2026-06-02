using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Model;

namespace Aizen.Modules.ReferenceData.Application.SystemParameter.Queries;

[DocumentationInfo("Returns the list of all system parameters", "Cached for 60 minutes; invalidated on system parameter changes.")]
public sealed class GetSystemParameterListQueryHandler : AizenQueryHandler<GetSystemParameterListQuery, IReadOnlyList<SystemParameterDto>>, IAizenQueryHandlerCacheable
{
    private readonly ISystemParameterReferenceService _service;

    public GetSystemParameterListQueryHandler(ISystemParameterReferenceService service)
    {
        _service = service;
    }

    public override async Task<IReadOnlyList<SystemParameterDto>> Handle(GetSystemParameterListQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetListAsync(request.OnlyActive, cancellationToken);
    }

    public AizenCacheType CacheType => AizenCacheType.Distributed;
    public AizenCacheOptions CacheOptions => new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60) };
}
