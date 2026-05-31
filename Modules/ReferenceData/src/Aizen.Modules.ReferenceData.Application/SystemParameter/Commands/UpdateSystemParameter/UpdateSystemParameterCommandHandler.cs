using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.SystemParameter.Commands;

public sealed class UpdateSystemParameterCommandHandler : AizenCommandHandler<UpdateSystemParameterCommand, SystemParameterDto>
{
    private readonly ISystemParameterReferenceService _service;

    public UpdateSystemParameterCommandHandler(ISystemParameterReferenceService service)
    {
        _service = service;
    }

    public override async Task<SystemParameterDto?> Handle(UpdateSystemParameterCommand request, CancellationToken cancellationToken)
    {
        return await _service.UpdateAsync(request.Key, request.Value, request.Description, request.IsActive, cancellationToken);
    }
}
