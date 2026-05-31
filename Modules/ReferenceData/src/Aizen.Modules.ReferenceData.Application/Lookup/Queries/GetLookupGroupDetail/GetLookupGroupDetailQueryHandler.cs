using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Queries;

public sealed class GetLookupGroupDetailQueryHandler : AizenQueryHandler<GetLookupGroupDetailQuery, LookupGroupDto?>
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
}
