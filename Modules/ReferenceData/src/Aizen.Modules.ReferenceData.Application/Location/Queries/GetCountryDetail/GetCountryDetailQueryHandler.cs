using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Queries;

public sealed class GetCountryDetailQueryHandler : AizenQueryHandler<GetCountryDetailQuery, CountryDto?>
{
    private readonly ILocationReferenceService _service;

    public GetCountryDetailQueryHandler(ILocationReferenceService service)
    {
        _service = service;
    }

    public override async Task<CountryDto?> Handle(GetCountryDetailQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetCountryAsync(request.CountryCode, cancellationToken);
    }
}
