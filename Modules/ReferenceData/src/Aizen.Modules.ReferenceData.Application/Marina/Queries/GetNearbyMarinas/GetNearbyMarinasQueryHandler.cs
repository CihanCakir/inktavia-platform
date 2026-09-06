using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Marina;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Marina.Queries;

[DocumentationInfo("Returns the closest marinas to a position", "Ordered by ascending great-circle distance; not cached (varies by exact coordinates).")]
public sealed class GetNearbyMarinasQueryHandler : AizenQueryHandler<GetNearbyMarinasQuery, IReadOnlyList<MarinaNearbyDto>>
{
    private readonly IMarinaReferenceService _service;

    public GetNearbyMarinasQueryHandler(IMarinaReferenceService service)
    {
        _service = service;
    }

    public override async Task<IReadOnlyList<MarinaNearbyDto>> Handle(GetNearbyMarinasQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetNearbyAsync(request.Latitude, request.Longitude, request.Limit, cancellationToken);
    }
}
