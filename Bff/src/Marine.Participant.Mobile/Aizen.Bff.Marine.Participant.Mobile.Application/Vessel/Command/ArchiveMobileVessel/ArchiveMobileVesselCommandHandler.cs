using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>
/// Archive orchestration (M4d). Thin owner-gated pass-through to the module's <c>PATCH /vessels/{id}/archive</c>.
/// Ownership is gated up front against the caller's default-page owned set (the same 0,20 key writes invalidate,
/// M4b lesson) so a foreign/unknown id returns a clean not-found rather than a 500 or another owner's vessel.
/// The module archives + invalidates the vessel and the default user list page, so the vessel drops from the
/// mobile active list immediately; this handler returns the re-read detail projection (IsArchived = true).
/// </summary>
public sealed class ArchiveMobileVesselCommandHandler
    : AizenCommandHandler<ArchiveMobileVesselCommand, MobileVesselDetailDto>
{
    private const int OwnedPageIndex = 0;
    private const int OwnedPageSize = 20;

    private readonly IParticipantProfileResolver _resolver;
    private readonly IVesselRemoteCall _vessel;

    public ArchiveMobileVesselCommandHandler(IParticipantProfileResolver resolver, IVesselRemoteCall vessel)
    {
        _resolver = resolver;
        _vessel = vessel;
    }

    public override async Task<MobileVesselDetailDto?> Handle(
        ArchiveMobileVesselCommand request, CancellationToken cancellationToken)
    {
        // Resolve → sets the identity holder so the owner-gated module write asserts as this participant.
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        // Ownership gate (clean not-found). Archived vessels remain in this unfiltered module read, so this
        // works before archive; it is the mobile list projection that hides archived rows, not this call.
        var ownedResp = await _vessel.GetUserVessels(OwnedPageIndex, OwnedPageSize);
        var owns = ownedResp?.Body?.Vessels?.Items?.Any(v => v.Id == request.VesselId) ?? false;
        if (!owns)
            throw new AizenBusinessException("Vessel not found.");

        var reason = ParseReason(request.Request?.Reason);
        var archiveResp = await _vessel.ArchiveVessel(request.VesselId, new ArchiveVesselRequest
        {
            Reason = reason,
            Notes = string.IsNullOrWhiteSpace(request.Request?.Notes) ? null : request.Request!.Notes!.Trim(),
        });
        if (archiveResp?.Body is null)
            throw new AizenBusinessException("Vessel could not be archived.");

        // Re-read detail (the module invalidated it) so the client mirrors exactly what persisted.
        var detail = (await _vessel.GetVesselDetail(request.VesselId))?.Body?.Vessel;
        if (detail is not null)
            return MobileVesselMapper.MapDetail(detail);

        throw new AizenBusinessException("Vessel archived but detail could not be read.");
    }

    private static VesselArchiveReason ParseReason(string? reason)
        => Enum.TryParse<VesselArchiveReason>(reason, ignoreCase: true, out var parsed)
            ? parsed
            : VesselArchiveReason.Other;
}
