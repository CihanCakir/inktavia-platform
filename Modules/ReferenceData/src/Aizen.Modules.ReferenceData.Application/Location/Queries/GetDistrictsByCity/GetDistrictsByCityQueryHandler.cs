using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Queries;

public sealed class GetDistrictsByCityQueryHandler : AizenQueryHandler<GetDistrictsByCityQuery, IReadOnlyList<DistrictDto>>
{
    private readonly ILocationReferenceService _service;

    public GetDistrictsByCityQueryHandler(ILocationReferenceService service)
    {
        _service = service;
    }

    public override async Task<IReadOnlyList<DistrictDto>> Handle(GetDistrictsByCityQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetDistrictsByCityAsync(request.CountryCode, request.CityCode, request.OnlyActive, cancellationToken);
    }
}
