using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Maintenance;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Maintenance;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Maintenance;

public sealed class UpsertMobileMaintenanceScheduleCommandHandler
    : AizenCommandHandler<UpsertMobileMaintenanceScheduleCommand, MobileMaintenanceUpsertResultDto>
{
    private const int OwnedPageSize = 100;

    private readonly IParticipantProfileResolver _resolver;
    private readonly IServiceRequestRemoteCall _sr;
    private readonly IVesselRemoteCall _vessel;

    public UpsertMobileMaintenanceScheduleCommandHandler(
        IParticipantProfileResolver resolver, IServiceRequestRemoteCall sr, IVesselRemoteCall vessel)
    {
        _resolver = resolver;
        _sr = sr;
        _vessel = vessel;
    }

    public override async Task<MobileMaintenanceUpsertResultDto?> Handle(
        UpsertMobileMaintenanceScheduleCommand request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var r = request.Request;
        if (r is null || r.VesselId <= 0 || string.IsNullOrWhiteSpace(r.ServiceCategoryCode))
            throw new AizenBusinessException("A maintenance schedule needs a vessel and a service category.");
        if (r.RecommendedIntervalMonths is int m && m < 1)
            throw new AizenBusinessException("The interval must be at least 1 month.");
        if (r.ReminderLeadDays is int lead && lead < 0)
            throw new AizenBusinessException("The reminder lead cannot be negative.");

        // Vessel-ownership gate: the vessel must be in the caller's own vessel set (the S12 upsert does NOT check this).
        var owned = await _vessel.GetUserVessels(0, OwnedPageSize);
        var ownsVessel = owned?.Body?.Vessels?.Items?.Any(v => v.Id == r.VesselId) ?? false;
        if (!ownsVessel)
            throw new AizenBusinessException("Vessel not found.");

        // OwnerUserId is intentionally left unset — the SR owner endpoint stamps it from the token.
        var resp = await _sr.UpsertOwnerMaintenanceSchedule(new UpsertMaintenanceScheduleRequest
        {
            VesselId                  = r.VesselId,
            ServiceCategoryCode       = r.ServiceCategoryCode.Trim(),
            ServiceTypeCode           = string.IsNullOrWhiteSpace(r.ServiceTypeCode) ? null : r.ServiceTypeCode!.Trim(),
            RecommendedIntervalMonths = r.RecommendedIntervalMonths,
            ReminderLeadDays          = r.ReminderLeadDays,
            LastPerformedAt           = r.LastPerformedAt,
            Notes                     = string.IsNullOrWhiteSpace(r.Notes) ? null : r.Notes!.Trim(),
        });

        var body = resp?.Body
            ?? throw new AizenBusinessException("Could not save the maintenance schedule.");

        return MobileMaintenanceMapper.MapUpsertResult(body);
    }
}
