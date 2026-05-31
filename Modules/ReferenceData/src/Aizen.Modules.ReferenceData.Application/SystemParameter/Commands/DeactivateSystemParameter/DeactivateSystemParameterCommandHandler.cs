using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.SystemParameter.Commands;

public sealed class DeactivateSystemParameterCommandHandler : AizenCommandHandler<DeactivateSystemParameterCommand, bool>
{
    private readonly ISystemParameterReferenceService _service;

    public DeactivateSystemParameterCommandHandler(ISystemParameterReferenceService service)
    {
        _service = service;
    }

    public override async Task<bool> Handle(DeactivateSystemParameterCommand request, CancellationToken cancellationToken)
    {
        await _service.DeactivateAsync(request.Key, cancellationToken);
        return true;
    }
}
