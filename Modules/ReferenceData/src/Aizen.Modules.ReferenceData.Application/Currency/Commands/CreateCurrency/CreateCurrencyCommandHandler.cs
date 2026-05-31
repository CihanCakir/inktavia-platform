using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Currency;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Currency.Commands;

public sealed class CreateCurrencyCommandHandler : AizenCommandHandler<CreateCurrencyCommand, CurrencyDto>
{
    private readonly ICurrencyReferenceService _service;

    public CreateCurrencyCommandHandler(ICurrencyReferenceService service)
    {
        _service = service;
    }

    public override async Task<CurrencyDto?> Handle(CreateCurrencyCommand request, CancellationToken cancellationToken)
    {
        return await _service.CreateAsync(request.Request, cancellationToken);
    }
}
