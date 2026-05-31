using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Currency.Commands;

public sealed class DeactivateCurrencyCommandHandler : AizenCommandHandler<DeactivateCurrencyCommand, bool>
{
    private readonly ICurrencyReferenceService _service;

    public DeactivateCurrencyCommandHandler(ICurrencyReferenceService service)
    {
        _service = service;
    }

    public override async Task<bool> Handle(DeactivateCurrencyCommand request, CancellationToken cancellationToken)
    {
        await _service.DeactivateAsync(request.Id, cancellationToken);
        return true;
    }
}
