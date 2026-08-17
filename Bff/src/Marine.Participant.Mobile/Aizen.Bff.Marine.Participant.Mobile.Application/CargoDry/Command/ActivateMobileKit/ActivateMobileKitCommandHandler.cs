using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.CargoDry;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.CargoDry;

/// <summary>
/// Activate a validated kit onto one of the caller's OWN vessels. Two gates before the module call:
///   1) Resolve the caller (sets the identity holder so the downstream activate asserts as this participant).
///   2) Confirm <c>VesselId</c> is in the caller's owned-vessel set (the module's activate has no owner check on the
///      vessel), reading the module's DEFAULT page (0,20) — the same key the vessel write-path invalidates — so a
///      just-created vessel is visible immediately. A foreign/unknown vessel → clean "Kit activation failed." and the
///      module is NEVER hit (never activate onto another owner's vessel).
/// The body carries no user id — the module stamps <c>UserId = UserInfo.UserId</c> from the asserted identity, so the
/// kit lands under the same id-space <c>GetMyKits</c> reads back. Module business errors (invalid/expired token,
/// already-activated serial) and a 401 (missing audience mapper) surface as clean mobile business errors, not 500s.
/// </summary>
public sealed class ActivateMobileKitCommandHandler
    : AizenCommandHandler<ActivateMobileKitCommand, MobileKitDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IVesselRemoteCall _vessel;
    private readonly ICargoDryRemoteCall _cargoDry;
    private readonly ILogger<ActivateMobileKitCommandHandler> _logger;

    public ActivateMobileKitCommandHandler(
        IParticipantProfileResolver resolver,
        IVesselRemoteCall vessel,
        ICargoDryRemoteCall cargoDry,
        ILogger<ActivateMobileKitCommandHandler> logger)
    {
        _resolver = resolver;
        _vessel = vessel;
        _cargoDry = cargoDry;
        _logger = logger;
    }

    public override async Task<MobileKitDto?> Handle(
        ActivateMobileKitCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ActivationToken))
            throw new AizenBusinessException("Activation token is required.");
        if (request.VesselId <= 0)
            throw new AizenBusinessException("A vessel must be selected.");

        // 1) Resolve → sets the identity holder (scopes the ownership lookup AND asserts the downstream activate).
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("Kit activation failed.");

        // 2) Vessel-ownership gate — the module never activates a kit it shouldn't, but its activate has no check that
        //    the vessel is the caller's, so gate here. Reuses the M4a owned-set read (default page = whole owned set).
        var listResp = await _vessel.GetUserVessels(0, 20);
        var owned = listResp?.Body?.Vessels?.Items?.Any(v => v.Id == request.VesselId) ?? false;
        if (!owned)
        {
            _logger.LogWarning("Rejected CargoDry activation onto non-owned vessel {VesselId} by profile {ProfileId}.",
                request.VesselId, resolution.ProfileId);
            throw new AizenBusinessException("Kit activation failed.");
        }

        try
        {
            var kit = await _cargoDry.ActivateKit(new ActivateKitRemoteRequest
            {
                ActivationToken = request.ActivationToken.Trim(),
                VesselId        = request.VesselId,
                Method          = request.Method,
            });

            if (kit is null)
                throw new AizenBusinessException("Kit activation failed.");

            return MobileCargoDryMapper.MapKit(kit);
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "CargoDry activate failed for vessel {VesselId} (status {Status}).",
                request.VesselId, ex.StatusCode);
            // Surface the module's OWN business message on a 400 (e.g. SR_CARGODRY_KIT_COMMERCIAL_REVIEW_REQUIRED,
            // invalid/expired token) so the app can map it to a friendly prompt; collapse everything else to a
            // generic error (never leak a 500/transport detail).
            var moduleMessage = ex.StatusCode == System.Net.HttpStatusCode.BadRequest
                ? ExtractModuleBusinessMessage(ex.Content)
                : null;
            throw new AizenBusinessException(
                string.IsNullOrWhiteSpace(moduleMessage) ? "Kit activation failed." : moduleMessage!);
        }
    }

    // The module wraps a business failure as AizenApiResponse&lt;NoContext&gt; (camelCase) — pull header.errorMessage.
    private static string? ExtractModuleBusinessMessage(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("header", out var header) &&
                header.TryGetProperty("errorMessage", out var msg) &&
                msg.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                return msg.GetString();
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // Not the expected envelope — fall back to the generic message.
        }
        return null;
    }
}
