using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.CargoDry;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.CargoDry;

/// <summary>
/// Owner-gated kit detail — mirrors <c>GetMobileVesselDetailQueryHandler</c>. There is NO owner-scoped module detail
/// endpoint (the admin one is unscoped and leaks QrPayload/ConsignmentAgreementId/RevokeReason), so detail is served
/// from the owner's OWN kit set: resolve identity → <c>GetMyKits</c> (already OwnerUserId-filtered) → find the id in
/// that set. Not present ⇒ clean "Kit not found." (never another owner's kit, never a 500, never the admin path).
/// VesselName is enriched from the caller's owned-vessel set (the module's GetMyKits doesn't populate it).
/// </summary>
public sealed class GetMobileMyKitDetailQueryHandler
    : AizenQueryHandler<GetMobileMyKitDetailQuery, MobileKitDetailDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly ICargoDryRemoteCall _cargoDry;
    private readonly IVesselRemoteCall _vessel;
    private readonly ILogger<GetMobileMyKitDetailQueryHandler> _logger;

    public GetMobileMyKitDetailQueryHandler(
        IParticipantProfileResolver resolver,
        ICargoDryRemoteCall cargoDry,
        IVesselRemoteCall vessel,
        ILogger<GetMobileMyKitDetailQueryHandler> logger)
    {
        _resolver = resolver;
        _cargoDry = cargoDry;
        _vessel = vessel;
        _logger = logger;
    }

    public override async Task<MobileKitDetailDto> Handle(
        GetMobileMyKitDetailQuery request, CancellationToken cancellationToken)
    {
        if (request.KitId <= 0)
            throw new AizenBusinessException("Kit not found.");

        // Resolve → sets the identity holder so the module scopes GetMyKits to this participant (same id as activate).
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("Kit not found.");

        CargoDryMyKitsRemoteResponse? mine;
        try
        {
            mine = await _cargoDry.GetMyKits();
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "CargoDry GetMyKits failed for kit-detail {KitId} (status {Status}).",
                request.KitId, ex.StatusCode);
            throw new AizenBusinessException("Kit not found.");
        }

        // Owned-set gate: the requested kit must be one of the caller's own kits.
        var kit = mine?.Items?.FirstOrDefault(k => k.Id == request.KitId)
            ?? throw new AizenBusinessException("Kit not found.");

        // Enrich VesselName from the caller's owned-vessel set (default page 0,20 = whole owned set), if the kit is
        // on a vessel. A failure to resolve is non-fatal — the detail is still returned with a null vessel name.
        string? vesselName = null;
        if (kit.VesselId is > 0)
        {
            try
            {
                var listResp = await _vessel.GetUserVessels(0, 20);
                vesselName = listResp?.Body?.Vessels?.Items?
                    .FirstOrDefault(v => v.Id == kit.VesselId)?.Name;
            }
            catch (Refit.ApiException ex)
            {
                _logger.LogWarning(ex, "Vessel-name enrich failed for kit {KitId} vessel {VesselId}.",
                    request.KitId, kit.VesselId);
            }
        }

        return MobileCargoDryMapper.MapDetail(kit, vesselName);
    }
}
