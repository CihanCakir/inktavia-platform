using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Currency.Commands;

public sealed class SetBaseCurrencyCommandHandler : AizenCommandHandler<SetBaseCurrencyCommand, CurrencyDto>
{
    private readonly ICurrencyReferenceService _service;

    public SetBaseCurrencyCommandHandler(ICurrencyReferenceService service)
    {
        _service = service;
    }

    public override async Task<CurrencyDto?> Handle(SetBaseCurrencyCommand request, CancellationToken cancellationToken)
    {
        return await _service.SetBaseCurrencyAsync(request.Id, cancellationToken);
    }
}
