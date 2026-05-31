using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Queries;

public sealed class GetLookupGroupListQueryHandler : AizenQueryHandler<GetLookupGroupListQuery, IReadOnlyList<LookupGroupDto>>
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
}
