using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.CargoDry;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;

/// <summary>
/// BFF → CargoDry module calls (BE_MO11a). Three owner-facing endpoints: validate a QR/serial, activate the
/// resulting token onto a vessel, and list the caller's kits. Unlike the Vessel module (which wraps every reply in
/// <c>AizenApiResponse&lt;T&gt;</c>), the CargoDry controllers return the raw DTO via <c>Ok(dto)</c> — so the typed
/// bodies here are the module DTOs directly, exactly as the AdminPanel BFF's CargoDry client does. Business failures
/// (invalid/expired token, already-activated serial) come back as HTTP 400 and a 401 (missing audience mapper) as
/// HTTP 401 — both surface as a Refit <c>ApiException</c> the feature handlers catch and re-throw as clean mobile
/// business errors.
///
/// Identity: the caller's asserted identity rides the shared auth-forwarding handler (Authorization service token +
/// X-Aizen-Bff-Assertion). The module derives <c>UserInfo.UserId</c> from that assertion for BOTH activate and list —
/// the bodies never carry an explicit user id, so the activate/list id-spaces are identical.
/// </summary>
public interface ICargoDryRemoteCall : IAizenRemoteCall
{
    // Anonymous + IP-rate-limited module endpoint; the BFF still forwards the caller's token (harmless).
    [AizenRemoteCallPost("/api/v1/cargodry/public/validate")]
    Task<CargoDryKitValidationDto> ValidateKit([AizenRemoteCallBody] ValidateKitRemoteRequest request);

    // Auth. The module stamps UserId = UserInfo.UserId (the asserted caller), Source = MobileApp, IP, UA.
    [AizenRemoteCallPost("/api/v1/cargodry/kits/activate")]
    Task<CargoDryKitDto> ActivateKit([AizenRemoteCallBody] ActivateKitRemoteRequest request);

    // Auth. GetMyKits filters by OwnerUserId = UserInfo.UserId (the same asserted caller as activate).
    [AizenRemoteCallGet("/api/v1/cargodry/kits")]
    Task<CargoDryMyKitsRemoteResponse> GetMyKits();
}
