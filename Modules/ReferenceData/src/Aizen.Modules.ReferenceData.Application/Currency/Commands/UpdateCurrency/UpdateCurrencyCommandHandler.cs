using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Currency.Commands;

public sealed class UpdateCurrencyCommandHandler : AizenCommandHandler<UpdateCurrencyCommand, CurrencyDto>
{
    private readonly ICurrencyReferenceService _service;

    public UpdateCurrencyCommandHandler(ICurrencyReferenceService service)
    {
        _service = service;
    }

    public override async Task<CurrencyDto?> Handle(UpdateCurrencyCommand request, CancellationToken cancellationToken)
    {
        return await _service.UpdateAsync(request.Id, request.Name, request.Symbol, request.DecimalPlaces, request.IsActive, cancellationToken);
    }
}
