using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.SystemParameter.Queries;

public sealed class GetSystemParameterByKeyQueryHandler : AizenQueryHandler<GetSystemParameterByKeyQuery, SystemParameterDto?>
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
}
