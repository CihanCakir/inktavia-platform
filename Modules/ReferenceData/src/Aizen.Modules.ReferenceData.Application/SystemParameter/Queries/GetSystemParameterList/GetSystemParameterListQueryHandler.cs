using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.SystemParameter.Queries;

public sealed class GetSystemParameterListQueryHandler : AizenQueryHandler<GetSystemParameterListQuery, IReadOnlyList<SystemParameterDto>>
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
}
