using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.SystemParameter.Commands;

public sealed class ActivateSystemParameterCommandHandler : AizenCommandHandler<ActivateSystemParameterCommand, bool>
{
    private readonly ISystemParameterReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public ActivateSystemParameterCommandHandler(ISystemParameterReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<bool> Handle(ActivateSystemParameterCommand request, CancellationToken cancellationToken)
    {
        await _service.ActivateAsync(request.Key, cancellationToken);
        await _invalidation.InvalidateSystemParameterAsync(request.Key, cancellationToken);
        return true;
    }
}
