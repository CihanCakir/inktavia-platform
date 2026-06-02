using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.SystemParameter.Commands;

public sealed class UpdateSystemParameterCommandHandler : AizenCommandHandler<UpdateSystemParameterCommand, SystemParameterDto>
{
    private readonly ISystemParameterReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public UpdateSystemParameterCommandHandler(ISystemParameterReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<SystemParameterDto?> Handle(UpdateSystemParameterCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(request.Key, request.Value, request.Description, request.IsActive, cancellationToken);
        await _invalidation.InvalidateSystemParameterAsync(request.Key, cancellationToken);
        return result;
    }
}
