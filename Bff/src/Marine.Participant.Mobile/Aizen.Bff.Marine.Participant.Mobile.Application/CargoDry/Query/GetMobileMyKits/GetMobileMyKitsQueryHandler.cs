using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.CargoDry;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.CargoDry;

/// <summary>List the caller's kits. Resolve first so the identity holder is set — the downstream GetMyKits asserts as
/// this participant, and the module filters <c>OwnerUserId = UserInfo.UserId</c> off that SAME asserted id the
/// activate used, so a just-activated kit appears here (the id-space trap is avoided). No participant profile ⇒ owns
/// nothing ⇒ an empty list, never a 500. A transport failure surfaces as a clean business error.</summary>
public sealed class GetMobileMyKitsQueryHandler
    : AizenQueryHandler<GetMobileMyKitsQuery, MobileMyKitsDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly ICargoDryRemoteCall _cargoDry;
    private readonly ILogger<GetMobileMyKitsQueryHandler> _logger;

    public GetMobileMyKitsQueryHandler(
        IParticipantProfileResolver resolver, ICargoDryRemoteCall cargoDry, ILogger<GetMobileMyKitsQueryHandler> logger)
    {
        _resolver = resolver;
        _cargoDry = cargoDry;
        _logger = logger;
    }

    public override async Task<MobileMyKitsDto> Handle(
        GetMobileMyKitsQuery request, CancellationToken cancellationToken)
    {
        // Resolve → sets the identity holder so the module scopes GetMyKits to this participant (same id as activate).
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            return new MobileMyKitsDto();

        try
        {
            var result = await _cargoDry.GetMyKits();
            return result is null ? new MobileMyKitsDto() : MobileCargoDryMapper.MapMyKits(result);
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "CargoDry GetMyKits failed for profile {ProfileId} (status {Status}).",
                resolution.ProfileId, ex.StatusCode);
            throw new AizenBusinessException("Could not load your kits.");
        }
    }
}
