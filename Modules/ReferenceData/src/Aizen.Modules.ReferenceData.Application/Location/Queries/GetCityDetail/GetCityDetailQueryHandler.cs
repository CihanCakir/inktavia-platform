using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Queries;

public sealed class GetCityDetailQueryHandler : AizenQueryHandler<GetCityDetailQuery, CityDto?>
{
    private readonly ILocationReferenceService _service;

    public GetCityDetailQueryHandler(ILocationReferenceService service)
    {
        _service = service;
    }

    public override async Task<CityDto?> Handle(GetCityDetailQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetCityAsync(request.CountryCode, request.CityCode, cancellationToken);
    }
}
