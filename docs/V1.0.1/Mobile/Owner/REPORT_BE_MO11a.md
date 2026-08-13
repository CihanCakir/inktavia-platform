# REPORT — BE_MO11a: Mobile BFF CargoDry surface (validate → activate → my kits)

> **Repo:** `addesso-project` — `Bff/src/Marine.Participant.Mobile`. Additive BFF vertical over the existing (live)
> CargoDry module owner endpoints. **Not committed** (per instruction).

## What shipped

A new owner-facing CargoDry surface on the mobile BFF, mirroring the existing feature style (M4a/M4b vessels):

| Layer | File | Notes |
|---|---|---|
| Remote client | `Application/Common/RemoteClients/ICargoDryRemoteCall.cs` | 3 typed calls; **returns the module DTOs directly** (see envelope note) |
| Contracts | `Application/Contracts/CargoDry/MobileCargoDryDtos.cs` | `MobileKitValidationDto`, `MobileKitDto`, `MobileMyKitsDto`, remote request/response records |
| Contracts | `Application/Contracts/CargoDry/MobileCargoDryRequests.cs` | Controller request bodies (no user id — identity is asserted) |
| Mapper | `Application/CargoDry/MobileCargoDryMapper.cs` | module DTO → mobile contract (enum status stringified) |
| Slice 1 | `Application/CargoDry/Command/ValidateMobileKit/*` | serial+batch required guard → `ValidateKit` |
| Slice 2 | `Application/CargoDry/Command/ActivateMobileKit/*` | **vessel-ownership guard** → `ActivateKit` (identity forwarded) |
| Slice 3 | `Application/CargoDry/Query/GetMobileMyKits/*` | resolve → `GetMyKits` (empty when no profile) |
| Controller | `Controllers/V1/CargoDryController.cs` | `[Authorize]`, `Route("api/v1/mobile/cargodry")` |
| DI | `Application/DependencyInjection.cs` | registers `ICargoDryRemoteCall` on the standard auth-forwarding chain |
| Config | `configuration/appsettings.json` + `.Development.json` | `RemoteCalls:ICargoDryRemoteCall:BaseUrl` |
| Compose | `docker-compose.yaml` | mobile BFF `depends_on` + `RemoteCalls__ICargoDryRemoteCall__BaseUrl`; **cargodry-api BffAssertion `+marine-mobile-bff`** |

### Envelope deviation from the task brief (important)
The brief suggested an `AizenResponse<T>`/`.Body` envelope (the Vessel-module pattern). **CargoDry does not use it** — its
controllers return the raw DTO via `Ok(dto)`, and the module's global exception middleware wraps only *failures* as
`AizenApiResponse<NoContext>` at HTTP 400/500. This was confirmed against the working AdminPanel BFF CargoDry client,
which likewise returns the DTOs directly (`Task<CargoDryValidationBffDto>`, `Task<CargoDryKitBffDto>`, …). So
`ICargoDryRemoteCall` binds the module DTOs directly; module business/transport failures come back as a Refit
`ApiException` (400/401/500) which each handler catches and re-throws as a clean mobile `AizenBusinessException` — never
a leaked 500.

### Identity — forward, don't map
The activate/list bodies carry **no user id**. The BFF forwards the owner's asserted identity (Authorization service
token + `X-Aizen-Bff-Assertion`) and the module derives `UserInfo.UserId` for both `ActivateKit` and `GetMyKits`, so the
two share one id-space. Prerequisites for the module to trust the assertion:
- **BffAssertion allowlist:** added `BffAssertion__AllowedClientIds__2: marine-mobile-bff` to `cargodry-api` (mirrors the
  MO1/MO9c/MO10a fixes). Without it the module ignores the assertion and keys on the wrong id.
- **Audience mapper:** the `audience-cargodry-api` `oidc-audience-mapper` was **already present** on the `marine-mobile-bff`
  Keycloak client in `infrastructure/keycloak/inktavia-realm-realm.json` — no realm change needed.

## Build & tests

- `dotnet build` mobile BFF host: **0 errors** (pre-existing CS8609 nullability warnings only).
- Unit tests (`Aizen.Bff.Marine.Participant.Mobile.UnitTests/CargoDry/`): **all 32 green** (8 new + 24 existing). No
  mocking library exists in the repo, so the doubles are hand-rolled (repo convention):
  1. validate maps IsValid/token through;
  2. activate onto an **owned** vessel succeeds and forwards identity — asserted via reflection that the wire body has
     **no `*User*`/`*Owner*` property**;
  3. activate onto a **non-owned** vessel → business error, downstream `ActivateKit` **never called** (`ActivateCallCount==0`);
  4. GetMyKits maps counts + items (enum status stringified);
  5. a downstream **401** (missing audience mapper) surfaces as a clean `AizenBusinessException`, not a 500;
  6. no-profile guards (activate rejected before any call; GetMyKits → empty).

## Live verification (docker stack, real Keycloak owner)

Rebuilt `bff-marine-mobile`, recreated it + `cargodry-api` (new env). Owner `qa.owner.aug5@inktavia.com` logged in via the
BFF password flow (resolved identity user id **100029**, 6 owned vessels).

1. **Audience mapper present**, BFF restarted → CargoDry calls authorize (no 401). ✓
2. `POST /api/v1/mobile/cargodry/kits/validate` `{CDK-PRV2-0001, 202507-CONS-PRV2}` → `isValid:true`, product "CargoDry
   Standard", 5-min `activationToken`. ✓
3. `POST /api/v1/mobile/cargodry/kits/activate` `{token, vesselId:100014}` (an **owned** vessel) → `success:true`, kit id 9
   `Activated` on vessel 100014. ✓
4. `GET /api/v1/mobile/cargodry/kits` → `total 1, active 1`, the just-activated kit present. DB confirms
   `CDK-PRV2-0001 → Status=Activated, OwnerUserId=100029, VesselId=100014`. **Identity id-space proven**: the module
   stamped the owner from the assertion and GetMyKits filtered by that *same* 100029. ✓
5. `POST …/activate` `{token(CDK-PRV2-0002), vesselId:999999}` (a **non-owned** vessel) → HTTP 400 "Kit activation
   failed."; DB confirms `CDK-PRV2-0002` still `Available` with no owner → the **module was never hit**. ✓

## Out-of-scope module observation (do-not-modify surface)

The dev-seeded platform kits with **`SalesChannel = null`** (e.g. `CDK-STAN-0001`) cannot be activated: the module's
`CargoDryKitEntity.Activate()` takes the Decision-N18 early-return (`Status = CommercialReviewRequired`) **without
setting `ActivatedAt`/`ExpiresAt`**, and `ActivateKitCommandHandler` (line 130) then NREs on `kit.ActivatedAt!.Value`
when publishing `CargoDryKitActivatedMessage` — the whole command rolls back. This is a **CargoDry module** defect
(caller doesn't handle the documented `CommercialReviewRequired` return path), not a BFF issue, and is left untouched per
MO11a's "BFF vertical only / do not modify the module" boundary. The BFF behaves correctly against it: the module 500 is
surfaced as a clean `"Kit activation failed."` business error. Activation succeeds for kits that carry a `SalesChannel`
(verified above). Recommend a follow-up module ticket.

## Next
MO11b — owner-gated kit detail + expiry read.
