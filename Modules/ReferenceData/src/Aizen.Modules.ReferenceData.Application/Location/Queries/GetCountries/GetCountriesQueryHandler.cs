using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Queries;

public sealed class GetCountriesQueryHandler : AizenQueryHandler<GetCountriesQuery, IReadOnlyList<CountryDto>>
{
    private readonly ILocationReferenceService _service;

    public GetCountriesQueryHandler(ILocationReferenceService service)
    {
        _service = service;
    }

    public override async Task<IReadOnlyList<CountryDto>> Handle(GetCountriesQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetCountriesAsync(request.OnlyActive, cancellationToken);
    }
}
