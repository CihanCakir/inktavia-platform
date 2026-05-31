using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.SystemParameter.Commands;

public sealed class CreateSystemParameterCommandHandler : AizenCommandHandler<CreateSystemParameterCommand, SystemParameterDto>
{
    private readonly ISystemParameterReferenceService _service;

    public CreateSystemParameterCommandHandler(ISystemParameterReferenceService service)
    {
        _service = service;
    }

    public override async Task<SystemParameterDto?> Handle(CreateSystemParameterCommand request, CancellationToken cancellationToken)
    {
        return await _service.CreateAsync(request.Key, request.Value, request.ValueType, request.Description, request.IsEncrypted, cancellationToken);
    }
}
