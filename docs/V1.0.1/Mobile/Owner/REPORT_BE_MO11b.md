# REPORT — BE_MO11b: Mobile BFF owner-gated kit detail + expiry read

> **Repo:** `addesso-project` — `Bff/src/Marine.Participant.Mobile`. Additive BFF vertical on top of MO11a.
> **Not committed** (per instruction). Cross-link: [[mo11_cargodry_owner_progress]], `REPORT_BE_MO11a.md`.

## What shipped

An owner-scoped **kit detail** endpoint (efficiency / expiry / renewal + vessel name + `IsExpiringSoon`), served from
the owner's OWN kit set — the admin detail endpoint is never exposed.

| Layer | File | Notes |
|---|---|---|
| Contract | `Application/Contracts/CargoDry/MobileCargoDryDtos.cs` | `+ MobileKitDetailDto` (curated; no admin fields; `+ VesselName`, `+ IsExpiringSoon`) |
| Mapper | `Application/CargoDry/MobileCargoDryMapper.cs` | `+ MapDetail(kit, vesselName)`; `IsExpiringSoon = Activated && DaysUntilExpiry <= 30` |
| Query slice | `Application/CargoDry/Query/GetMobileMyKitDetail/*` | owned-set gate → curated dto → vessel-name enrich |
| Controller | `Controllers/V1/CargoDryController.cs` | `+ GET kits/{kitId:long}` → `GetMobileMyKitDetailQuery` |

### Ownership rule (no admin endpoint, no IDOR)
The module's `GetCargoDryKitDetail` is **admin-only, owner-unscoped**, and `CargoDryKitDetailDto` leaks admin-sensitive
fields (`QrPayload`, `ConsignmentAgreementId`, `RevokeReason`, audit stamps). MO11b **does not** touch it. Detail is
served from the owner's own set, mirroring `GetMobileVesselDetailQueryHandler`:
1. resolve identity (asserted owner, same forward-only rule as MO11a);
2. `GetMyKits` (already `OwnerUserId`-filtered) → **find the requested kit id in that set**;
3. not present → clean `AizenBusinessException("Kit not found.")` (never another owner's kit, never a 500, never admin);
4. return a curated `MobileKitDetailDto` — no admin fields.

No new module endpoint is needed: the `GetMyKits` item already carries `EfficiencyPercent`, `DaysUntilExpiry`,
`ExpiresAt`, `RenewalCount`, `Status`, `VesselId`, `ActivatedAt`, `ProductName`. The detail is the owned list-item
enriched.

### Enrich & expiry
- **VesselName** — the module's `GetMyKits` doesn't populate it, so the BFF resolves it from the caller's owned-vessel
  set (`IVesselRemoteCall.GetUserVessels(0,20)`, the same owned-set the MO11a activate-guard uses); null if not found or
  the enrich call fails (non-fatal — detail still returns).
- **IsExpiringSoon** — `Status == Activated && DaysUntilExpiry <= 30`, mirroring the module's `ExpiringCount` threshold,
  so the FE renders the "expiring in N days" banner without re-deriving the rule.

### Scope decisions
- **Renewal purchase/checkout: OUT** (owner has no payment surface in MVP; renewal is provider/admin-automated). No owner
  renewal-pay command added.
- **Lifecycle timeline (§3, optional): DEFERRED to MO11c.** The module's history endpoints are **admin-routed**
  (`/api/v1/cargodry/admin/kits/{id}/history`), so an owner-safe timeline needs either a new module endpoint or calling
  an admin route — real added cost the doc explicitly said to defer. The core detail (efficiency/expiry/renewal) is
  enough for the owner MVP.
- **Deep-link target** — the expiry/renewal reminder push (N-F) should deep-link here (`ReferenceType=CargoDryKit`,
  `ReferenceId=kitId`); the FE tap-routing lands in MO11c.

## Build & tests
- `dotnet build` mobile BFF host: **0 errors**.
- Unit tests (`…UnitTests/CargoDry/`): **41 green** (9 new MO11b + 32 prior). New (hand-rolled doubles, repo convention):
  1. detail for an **owned** kit → curated dto with correct efficiency/expiry/renewal **and no admin-sensitive props**
     (asserted via reflection: no QrPayload/Consignment/Revoke/CommercialModel/ProviderProfileId);
  2. detail for a **non-owned** kit id → "Kit not found";
  3. `IsExpiringSoon` Theory — true at `Activated && ≤30`, false at 31 / Expired / Available (5 cases);
  4. vessel-name enrich → resolves for an owned vessel, **null** when the kit's vessel isn't owned;
  5. no-profile guard → not found (no downstream calls).

## Live verification (docker stack, real Keycloak owner `qa.owner.aug5@inktavia.com`)
Rebuilt + recreated `bff-marine-mobile`.
- `GET /api/v1/mobile/cargodry/kits/9` (owned, CDK-PRV2-0001 on vessel 100014) → **HTTP 200**: efficiency 99.9%,
  `daysUntilExpiry 90`, `renewalCount 0`, **`vesselName "FinalLoop2 78502"`** (enriched from the owned-vessel set),
  `isExpiringSoon false` (90 > 30). ✓
- `GET …/kits/999999` (non-owned) → **HTTP 400 "Kit not found."**, the admin path never hit. ✓

## Files (uncommitted)
- `Application/Contracts/CargoDry/MobileCargoDryDtos.cs` (+`MobileKitDetailDto`)
- `Application/CargoDry/MobileCargoDryMapper.cs` (+`MapDetail`)
- `Application/CargoDry/Query/GetMobileMyKitDetail/{Query,Handler}.cs`
- `Controllers/V1/CargoDryController.cs` (+`GET kits/{kitId}`)
- `…UnitTests/CargoDry/MobileCargoDryDetailTests.cs`

## Next
MO11c — RN/Expo CargoDry tab (scan → activate → my kits → **detail**) + `CargoDryKit` push deep-link routing.
