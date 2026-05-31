using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.SystemParameter.Commands;

public sealed class ActivateSystemParameterCommandHandler : AizenCommandHandler<ActivateSystemParameterCommand, bool>
{
    private readonly ISystemParameterReferenceService _service;

    public ActivateSystemParameterCommandHandler(ISystemParameterReferenceService service)
    {
        _service = service;
    }

    public override async Task<bool> Handle(ActivateSystemParameterCommand request, CancellationToken cancellationToken)
    {
        await _service.ActivateAsync(request.Key, cancellationToken);
        return true;
    }
}
