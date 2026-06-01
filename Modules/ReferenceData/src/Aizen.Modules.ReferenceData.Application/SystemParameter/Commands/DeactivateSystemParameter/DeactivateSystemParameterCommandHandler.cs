using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.SystemParameter.Commands;

public sealed class DeactivateSystemParameterCommandHandler : AizenCommandHandler<DeactivateSystemParameterCommand, bool>
{
    private readonly ISystemParameterReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public DeactivateSystemParameterCommandHandler(ISystemParameterReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<bool> Handle(DeactivateSystemParameterCommand request, CancellationToken cancellationToken)
    {
        await _service.DeactivateAsync(request.Key, cancellationToken);
        await _invalidation.InvalidateSystemParameterAsync(request.Key, cancellationToken);
        return true;
    }
}
