using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.SystemParameter.Queries;

public sealed class GetSystemParametersByPrefixQueryHandler : AizenQueryHandler<GetSystemParametersByPrefixQuery, IReadOnlyList<SystemParameterDto>>
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
}
