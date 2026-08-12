# BE_MO11a — Mobile BFF CargoDry surface (validate + activate + my kits)

> **Repo:** `addesso-project` — `Bff/src/Marine.Participant.Mobile`. Surface the **existing** CargoDry module owner
> endpoints through the owner mobile BFF: **validate a QR → activate onto a vessel → list my kits**. The module backend
> is done; this is the BFF vertical only. Additive, mirrors the existing mobile BFF feature style. **Do not commit.**

## Downstream module endpoints (already live — do not modify)
- `POST /api/v1/cargodry/public/validate` — anonymous, `EnableRateLimiting("validate-ip")`. Body
  `{ SerialNumber, BatchCode, Signature? }` → `CargoDryKitValidationDto { IsValid, InvalidReason?, ProductName?,
  ProductCode?, ValidityDays, HasSmartDevice, ActivationToken?, TokenExpiresAt? }` (5-min token).
- `POST /api/v1/cargodry/kits/activate` — auth. Body `{ ActivationToken, VesselId, Method }`; the **module** stamps
  `UserId = UserInfo.UserId`, `Source = MobileApp`, IP, UA. → activated `CargoDryKitDto`.
- `GET /api/v1/cargodry/kits` — auth. `GetMyKits` filters by `OwnerUserId = UserInfo.UserId`. →
  `{ Items: CargoDryKitDto[], Total, ActiveCount, ExpiringCount }`.

## Prerequisite — audience mapper (or everything 401s)
Add an **`oidc-audience-mapper` for `cargodry-api`** on the `marine-mobile-bff` Keycloak client, mirroring the existing
per-module mappers (vessel-api, serviceRequest, notification-api, …). Without it the BFF's service token lacks the
`cargodry-api` audience → module returns 401 → BFF surfaces 500/911 ([[provider_bff_module_audience_mapper]]). **Restart
the BFF** after adding it to bust the cached service token. (Config/env — surfaced, not committed with secrets.)

## Identity rule — forward, don't map
CargoDry keys `OwnerUserId` on the module-resolved `UserInfo.UserId`. The BFF must forward the **owner's asserted
identity** to `cargodry-api` and let the module derive `UserId` — for **both** activate and list — exactly as the
module controller does. **Do not** pass a participant-profile-id (or any explicit user id) as the CargoDry user; the
activate-vs-list id-spaces must be identical or activated kits won't appear in the list ([[provider_bff_module_call_auth]]).

## 1. Refit remote client
`Application/Common/RemoteClients/ICargoDryRemoteCall.cs` — mirror `IVesselRemoteCall` (typed concrete bodies, the
`AizenResponse<T>`/`.Body` envelope this BFF uses; **no `object`/`JsonElement`** — [[provider_bff_typed_body_required]]):
```csharp
public interface ICargoDryRemoteCall
{
    [Post("/api/v1/cargodry/public/validate")]
    Task<AizenResponse<CargoDryKitValidationBody>> ValidateKit([Body] ValidateKitRemoteRequest req);

    [Post("/api/v1/cargodry/kits/activate")]
    Task<AizenResponse<CargoDryKitBody>> ActivateKit([Body] ActivateKitRemoteRequest req);

    [Get("/api/v1/cargodry/kits")]
    Task<AizenResponse<GetMyKitsBody>> GetMyKits();
}
```
Register it in `DependencyInjection.cs` with the same Refit + auth-forwarding handler chain the other module clients
use, pointing at the `cargodry-api` base address.

## 2. Feature folder `Application/CargoDry/` — three slices (CQRS, one folder per command/query)
- **`Command/ValidateMobileKit`** — `{ SerialNumber, BatchCode, Signature? }` → calls `ValidateKit` → maps to
  `MobileKitValidationDto`. FluentValidation: serial + batch required. (The mobile endpoint itself stays behind normal
  mobile auth so only signed-in owners scan; the downstream is anonymous but IP-rate-limited.)
- **`Command/ActivateMobileKit`** — `{ ActivationToken, VesselId, Method = QrScan }`.
  - **Vessel-ownership guard (required):** resolve the caller (the `IParticipantProfileResolver` used elsewhere) and
    confirm `VesselId` is in the caller's owned-vessel set (reuse the `IVesselRemoteCall.GetUserVessels(0,20)` owned-set
    check from `GetMobileVesselDetailQueryHandler`). If not owned → `AizenBusinessException("Kit activation failed.")`
    (never activate onto another owner's vessel). Then call `ActivateKit` (identity forwarded) → map to `MobileKitDto`.
  - Surface the module's business errors (invalid/expired token, already-activated serial) as clean mobile business
    errors, not 500s.
- **`Query/GetMobileMyKits`** — calls `GetMyKits` → maps `{ Items, Total, ActiveCount, ExpiringCount }` to a mobile
  contract (`MobileMyKitsDto`). No paging needed (an owner holds a handful of kits).

## 3. Contracts `Application/Contracts/CargoDry/`
`MobileKitValidationDto`, `MobileKitDto` (subset of `CargoDryKitDto` the app needs: Id, SerialNumber, KitCode,
ProductName, Status, VesselId, VesselName?, ActivatedAt, ExpiresAt, EfficiencyPercent, DaysUntilExpiry, RenewalCount,
HasSmartDevice), `MobileMyKitsDto { Items, Total, ActiveCount, ExpiringCount }`, and the remote request/body records.
Add a `MobileCargoDryMapper` (mirror `MobileVesselMapper`).

## 4. Controller
`Aizen.Bff.Marine.Participant.Mobile/Controllers/CargoDryController.cs` — `[Authorize]`, `Route("api/mobile/cargodry")`:
- `POST kits/validate` → `ValidateMobileKitCommand`
- `POST kits/activate` → `ActivateMobileKitCommand`
- `GET kits` → `GetMobileMyKitsQuery`

Identity comes from the asserted mobile token, never the body.

## Don't-break / QA
- Additive: new remote client + feature + contracts + controller + one audience mapper. No module, no other BFF feature
  touched. Typed bodies. UTC timestamps pass through.
- **Unit tests** (mirror the mobile BFF test project): (1) validate maps IsValid/token through; (2) activate with an
  **owned** vessel succeeds and forwards identity (assert no explicit user id is sent in the body); (3) activate with a
  **non-owned** vessel → business "not found"/"failed", downstream `ActivateKit` **never called**; (4) GetMyKits maps
  counts; (5) a downstream 401 (missing audience mapper) surfaces as a clean error, not an unhandled 500.
- **Build:** `dotnet build` the mobile BFF solution 0 errors; the mobile BFF unit-test project green.

## Live verification
1. Confirm the `cargodry-api` audience mapper is present (BFF restarted).
2. As the owner (`qa.owner.aug5@inktavia.com`), `POST /api/mobile/cargodry/kits/validate` with a seeded valid
   serial/batch → `IsValid=true` + a token.
3. `POST /api/mobile/cargodry/kits/activate` with that token + an **owned** vessel id → activated kit returned.
4. `GET /api/mobile/cargodry/kits` → the just-activated kit appears (proves the activate/list identity is the **same**
   Identity user id — the trap is avoided). Try an activate onto a **non-owned** vessel → rejected, module not hit.

## Report
`docs/V1.0.1/Mobile/Owner/REPORT_BE_MO11a.md`: the remote client + three slices + audience-mapper note, the
identity-forwarding proof (validate→activate→list round-trip shows the kit, no explicit user id in bodies), the
vessel-ownership guard test, and build/test results. Then MO11b (owner-gated kit detail + expiry read).
