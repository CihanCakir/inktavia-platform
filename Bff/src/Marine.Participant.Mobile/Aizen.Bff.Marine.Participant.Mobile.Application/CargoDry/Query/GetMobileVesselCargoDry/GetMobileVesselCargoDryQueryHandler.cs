using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.CargoDry;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.CargoDry;

/// <summary>
/// Per-vessel CargoDry summary for the vessel screen (BE_MO11a read-path fix). There is no vessel-scoped module
/// endpoint, so — exactly like <c>GetMobileMyKitDetailQueryHandler</c> — this serves from the caller's OWN kit set:
/// resolve identity → <c>GetMyKits</c> (already OwnerUserId-filtered) → keep the kits whose <c>VesselId</c> matches.
/// A foreign/unknown vessel simply yields an empty summary (activeCount 0) rather than leaking anything or 500-ing.
/// No participant profile ⇒ owns nothing ⇒ empty. A transport failure surfaces as a clean business error.
/// </summary>
public sealed class GetMobileVesselCargoDryQueryHandler
    : AizenQueryHandler<GetMobileVesselCargoDryQuery, MobileVesselCargoDryDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly ICargoDryRemoteCall _cargoDry;
    private readonly ILogger<GetMobileVesselCargoDryQueryHandler> _logger;

    public GetMobileVesselCargoDryQueryHandler(
        IParticipantProfileResolver resolver,
        ICargoDryRemoteCall cargoDry,
        ILogger<GetMobileVesselCargoDryQueryHandler> logger)
    {
        _resolver = resolver;
        _cargoDry = cargoDry;
        _logger = logger;
    }

    public override async Task<MobileVesselCargoDryDto> Handle(
        GetMobileVesselCargoDryQuery request, CancellationToken cancellationToken)
    {
        if (request.VesselId <= 0)
            return new MobileVesselCargoDryDto();

        // Resolve → sets the identity holder so the module scopes GetMyKits to this participant (same id as activate).
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            return new MobileVesselCargoDryDto();

        try
        {
            var mine = await _cargoDry.GetMyKits();
            return mine is null
                ? new MobileVesselCargoDryDto()
                : MobileCargoDryMapper.MapVesselCargoDry(request.VesselId, mine);
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "CargoDry GetMyKits failed for vessel {VesselId} (status {Status}).",
                request.VesselId, ex.StatusCode);
            throw new AizenBusinessException("Could not load CargoDry protection for this vessel.");
        }
    }
}
