using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.WorkLog;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Command.WorkLog;

[DocumentationInfo("Update work phase command handler", "Finds the work phase by phase number and updates its progress and status.")]
public sealed class UpdateWorkPhaseCommandHandler : AizenCommandHandler<UpdateWorkPhaseCommand, UpdateWorkPhaseResponse>
{
    private readonly IWorkPhaseRepository _workPhaseRepository;

    public UpdateWorkPhaseCommandHandler(IWorkPhaseRepository workPhaseRepository)
    {
        _workPhaseRepository = workPhaseRepository;
    }

    public override async Task<UpdateWorkPhaseResponse?> Handle(UpdateWorkPhaseCommand request, CancellationToken cancellationToken)
    {
        var phase = await _workPhaseRepository.GetByPhaseNumberAsync(
            request.ServiceRequestId, request.PhaseNumber, cancellationToken)
            ?? throw new InvalidOperationException(
                $"WorkPhase {request.PhaseNumber} not found on ServiceRequest {request.ServiceRequestId}.");

        phase.UpdateProgress(request.Request.ProgressPercent, request.Request.Status);
        _workPhaseRepository.Update(phase);

        return new UpdateWorkPhaseResponse(phase.PhaseNumber, phase.ProgressPercent, phase.Status);
    }
}
