using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Currency.Commands;

public sealed class ActivateCurrencyCommandHandler : AizenCommandHandler<ActivateCurrencyCommand, bool>
{
    private readonly ICurrencyReferenceService _service;

    public ActivateCurrencyCommandHandler(ICurrencyReferenceService service)
    {
        _service = service;
    }

    public override async Task<bool> Handle(ActivateCurrencyCommand request, CancellationToken cancellationToken)
    {
        await _service.ActivateAsync(request.Id, cancellationToken);
        return true;
    }
}
