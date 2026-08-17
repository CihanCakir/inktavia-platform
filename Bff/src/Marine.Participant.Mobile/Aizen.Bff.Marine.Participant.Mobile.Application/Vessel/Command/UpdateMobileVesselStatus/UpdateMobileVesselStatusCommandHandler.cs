using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>
/// Status-change orchestration (M4d). Owner-gated pass-through to the module's <c>PATCH /vessels/{id}/status</c>.
/// The user-settable states are Active / Passive / UnderMaintenance; the module enforces the valid-transition
/// graph and rejects Draft/Sold/Archived (system- or archive-driven). Ownership is gated up front (clean
/// not-found). The module invalidates the vessel detail + status history (not the list — operational status does
/// not change list membership), so detail reflects the new status immediately; this returns the re-read detail.
/// </summary>
public sealed class UpdateMobileVesselStatusCommandHandler
    : AizenCommandHandler<UpdateMobileVesselStatusCommand, MobileVesselDetailDto>
{
    private const int OwnedPageIndex = 0;
    private const int OwnedPageSize = 20;

    private readonly IParticipantProfileResolver _resolver;
    private readonly IVesselRemoteCall _vessel;

    public UpdateMobileVesselStatusCommandHandler(IParticipantProfileResolver resolver, IVesselRemoteCall vessel)
    {
        _resolver = resolver;
        _vessel = vessel;
    }

    public override async Task<MobileVesselDetailDto?> Handle(
        UpdateMobileVesselStatusCommand request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        if (!TryParseStatus(request.Request?.Status, out var status))
            throw new AizenBusinessException(
                "A valid status is required (Active, Passive or UnderMaintenance).");

        var ownedResp = await _vessel.GetUserVessels(OwnedPageIndex, OwnedPageSize);
        var owns = ownedResp?.Body?.Vessels?.Items?.Any(v => v.Id == request.VesselId) ?? false;
        if (!owns)
            throw new AizenBusinessException("Vessel not found.");

        var statusResp = await _vessel.UpdateStatus(request.VesselId, new UpdateVesselStatusRequest
        {
            Status = status,
            Reason = string.IsNullOrWhiteSpace(request.Request?.Reason) ? null : request.Request!.Reason!.Trim(),
        });
        if (statusResp?.Body is null)
            throw new AizenBusinessException("Vessel status could not be changed.");

        var detail = (await _vessel.GetVesselDetail(request.VesselId))?.Body?.Vessel;
        if (detail is not null)
            return MobileVesselMapper.MapDetail(detail);

        throw new AizenBusinessException("Vessel status changed but detail could not be read.");
    }

    // User-settable operational states only — the module rejects any other transition, but reject the
    // archive-/system-owned states here too so the error is a clear 4xx-style business message, not a 500.
    private static bool TryParseStatus(string? raw, out VesselStatus status)
    {
        status = default;
        if (!Enum.TryParse(raw, ignoreCase: true, out VesselStatus parsed))
            return false;

        if (parsed is not (VesselStatus.Active or VesselStatus.Passive or VesselStatus.UnderMaintenance))
            return false;

        status = parsed;
        return true;
    }
}
