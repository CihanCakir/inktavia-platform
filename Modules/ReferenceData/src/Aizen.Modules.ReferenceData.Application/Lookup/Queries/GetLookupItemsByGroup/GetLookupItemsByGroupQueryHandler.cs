using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Lookup.Queries;

public sealed class GetLookupItemsByGroupQueryHandler : AizenQueryHandler<GetLookupItemsByGroupQuery, IReadOnlyList<LookupItemDto>>
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
}
