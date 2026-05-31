using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Location.Commands;

public sealed class UpdateCountryCommandHandler : AizenCommandHandler<UpdateCountryCommand, CountryDto>
{
    private readonly ILocationReferenceService _service;

    public UpdateCountryCommandHandler(ILocationReferenceService service)
    {
        _service = service;
    }

    public override async Task<CountryDto?> Handle(UpdateCountryCommand request, CancellationToken cancellationToken)
    {
        return await _service.UpdateCountryAsync(request.CountryCode, request.DefaultCurrencyCode, request.PhoneCode, request.IsActive, cancellationToken);
    }
}
