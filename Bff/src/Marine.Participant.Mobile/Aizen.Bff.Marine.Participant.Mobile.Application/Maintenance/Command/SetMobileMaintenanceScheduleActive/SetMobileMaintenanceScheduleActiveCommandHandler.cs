using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Maintenance;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Maintenance;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Maintenance;

public sealed class SetMobileMaintenanceScheduleActiveCommandHandler
    : AizenCommandHandler<SetMobileMaintenanceScheduleActiveCommand, MobileMaintenanceSetActiveResultDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IServiceRequestRemoteCall _sr;

    public SetMobileMaintenanceScheduleActiveCommandHandler(
        IParticipantProfileResolver resolver, IServiceRequestRemoteCall sr)
    {
        _resolver = resolver;
        _sr = sr;
    }

    public override async Task<MobileMaintenanceSetActiveResultDto?> Handle(
        SetMobileMaintenanceScheduleActiveCommand request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var resp = await _sr.SetOwnerMaintenanceScheduleActive(
            request.ScheduleId, new SetMaintenanceScheduleActiveRequest { IsActive = request.IsActive });

        var body = resp?.Body
            ?? throw new AizenBusinessException("Could not update the maintenance schedule.");

        return MobileMaintenanceMapper.MapSetActiveResult(body);
    }
}
