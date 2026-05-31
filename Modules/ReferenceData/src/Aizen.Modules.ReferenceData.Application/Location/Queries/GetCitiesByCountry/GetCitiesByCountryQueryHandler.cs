using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Queries;

public sealed class GetCitiesByCountryQueryHandler : AizenQueryHandler<GetCitiesByCountryQuery, IReadOnlyList<CityDto>>
{
    private readonly ILocationReferenceService _service;

    public GetCitiesByCountryQueryHandler(ILocationReferenceService service)
    {
        _service = service;
    }

    public override async Task<IReadOnlyList<CityDto>> Handle(GetCitiesByCountryQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetCitiesByCountryAsync(request.CountryCode, request.OnlyActive, cancellationToken);
    }
}
