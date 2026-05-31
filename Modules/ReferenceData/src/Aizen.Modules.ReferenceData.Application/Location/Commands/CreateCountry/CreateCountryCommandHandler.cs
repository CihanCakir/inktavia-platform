using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class CreateCountryCommandHandler : AizenCommandHandler<CreateCountryCommand, CountryDto>
{
    private readonly ILocationReferenceService _service;

    public CreateCountryCommandHandler(ILocationReferenceService service)
    {
        _service = service;
    }

    public override async Task<CountryDto?> Handle(CreateCountryCommand request, CancellationToken cancellationToken)
    {
        return await _service.CreateCountryAsync(request.Request, cancellationToken);
    }
}
