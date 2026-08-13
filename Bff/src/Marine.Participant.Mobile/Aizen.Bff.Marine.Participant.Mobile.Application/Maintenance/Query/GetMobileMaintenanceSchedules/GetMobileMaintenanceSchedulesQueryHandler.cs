using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Maintenance;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Maintenance;

public sealed class GetMobileMaintenanceSchedulesQueryHandler
    : AizenQueryHandler<GetMobileMaintenanceSchedulesQuery, MobileMaintenanceScheduleListDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IServiceRequestRemoteCall _sr;

    public GetMobileMaintenanceSchedulesQueryHandler(
        IParticipantProfileResolver resolver, IServiceRequestRemoteCall sr)
    {
        _resolver = resolver;
        _sr = sr;
    }

    public override async Task<MobileMaintenanceScheduleListDto?> Handle(
        GetMobileMaintenanceSchedulesQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var resp = await _sr.GetOwnerMaintenanceSchedules(request.VesselId, request.IncludeInactive);
        return MobileMaintenanceMapper.MapList(resp?.Body);
    }
}
