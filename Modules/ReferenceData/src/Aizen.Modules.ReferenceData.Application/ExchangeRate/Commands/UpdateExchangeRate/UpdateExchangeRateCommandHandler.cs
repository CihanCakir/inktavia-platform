using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.ExchangeRate;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.ExchangeRate.Commands;

public sealed class UpdateExchangeRateCommandHandler : AizenCommandHandler<UpdateExchangeRateCommand, ExchangeRateDto>
{
    private readonly IExchangeRateReferenceService _service;

    public UpdateExchangeRateCommandHandler(IExchangeRateReferenceService service)
    {
        _service = service;
    }

    public override async Task<ExchangeRateDto?> Handle(UpdateExchangeRateCommand request, CancellationToken cancellationToken)
    {
        return await _service.UpdateRateAsync(request.Request, cancellationToken);
    }
}
