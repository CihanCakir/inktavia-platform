using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Queries;

public sealed class GetLookupGroupTreeQueryHandler : AizenQueryHandler<GetLookupGroupTreeQuery, IReadOnlyList<LookupGroupTreeDto>>
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
}
