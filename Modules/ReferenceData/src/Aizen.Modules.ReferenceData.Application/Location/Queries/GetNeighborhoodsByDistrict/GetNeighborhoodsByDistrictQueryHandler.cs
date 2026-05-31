using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Queries;

public sealed class GetNeighborhoodsByDistrictQueryHandler : AizenQueryHandler<GetNeighborhoodsByDistrictQuery, IReadOnlyList<NeighborhoodDto>>
{
    private readonly ILocationReferenceService _service;

    public GetNeighborhoodsByDistrictQueryHandler(ILocationReferenceService service)
    {
        _service = service;
    }

    public override async Task<IReadOnlyList<NeighborhoodDto>> Handle(GetNeighborhoodsByDistrictQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetNeighborhoodsByDistrictAsync(request.CountryCode, request.CityCode, request.DistrictCode, request.OnlyActive, cancellationToken);
    }
}
