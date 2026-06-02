using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.SystemParameter.Commands;

public sealed class CreateSystemParameterCommandHandler : AizenCommandHandler<CreateSystemParameterCommand, SystemParameterDto>
{
    private readonly ISystemParameterReferenceService _service;
    private readonly IReferenceDataCacheInvalidationService _invalidation;

    public CreateSystemParameterCommandHandler(ISystemParameterReferenceService service, IReferenceDataCacheInvalidationService invalidation)
    {
        _service = service;
        _invalidation = invalidation;
    }

    public override async Task<SystemParameterDto?> Handle(CreateSystemParameterCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request.Key, request.Value, request.ValueType, request.Description, request.IsEncrypted, cancellationToken);
        await _invalidation.InvalidateSystemParameterAsync(cancellationToken: cancellationToken);
        return result;
    }
}
