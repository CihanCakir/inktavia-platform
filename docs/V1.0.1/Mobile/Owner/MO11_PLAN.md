# MO11 — CargoDry owner flow (phased plan)

> **The last owner-track feature.** Give the boat owner the CargoDry kit journey on the mobile app: **scan/validate a
> QR → activate onto a vessel → see my kits → kit detail (efficiency / expiry / renewal reminder)**. Owner surfaces
> are **cost-free** (no owner-side payment in MVP). **Do not commit** (per-task commits by the owner).

## What already exists (don't rebuild)
The **CargoDry module** backend for owner activation is already implemented and correct:
- `POST /api/v1/cargodry/public/validate` — **anonymous**, IP-rate-limited (`validate-ip`). Validates serial/batch(+signature)
  → returns `CargoDryKitValidationDto { IsValid, InvalidReason, ProductName, ValidityDays, HasSmartDevice,
  ActivationToken, TokenExpiresAt }` — a **5-minute activation token**.
- `POST /api/v1/cargodry/kits/activate` — **auth**. Body `{ ActivationToken, VesselId, Method }`; the controller stamps
  `UserId = UserInfo.UserId`, `Source = MobileApp`, IP + UA. Returns the activated `CargoDryKitDto`.
- `GET /api/v1/cargodry/kits` — **auth**. `GetMyKits` filters by `OwnerUserId = UserInfo.UserId`; returns
  `{ Items: CargoDryKitDto[], Total, ActiveCount, ExpiringCount(≤30d) }` with `EfficiencyPercent`, `DaysUntilExpiry`,
  `ExpiresAt`, `RenewalCount`, `VesselId`, `Status`.

**The gap is purely the surface:** the `Marine.Participant.Mobile` BFF has **no CargoDry feature** (it has Vessel,
ServiceRequest, Maintenance, Chat, Notification, Me, Reference, Upload, Membership), and the RN/Expo app has no CargoDry
screens. MO11 wires the BFF + FE onto the existing module endpoints.

## Two correctness traps to respect (both flagged from code)
1. **Owner identity = Identity user id, NOT participant profile id.** CargoDry keys `OwnerUserId` on
   `UserInfo.UserId` (the module controller reads it off the asserted token for **both** activate and list). This is
   internally consistent **only if the BFF forwards the same asserted owner identity** to `cargodry-api` for activate
   **and** list — and never passes an explicit participant-profile-id as the CargoDry user id. Mixing the two id-spaces
   is exactly the owner-id trap that bit SR/notifications ([[bff_providername_enrichment]] neighborhood); here the fix
   is to **not** map — just forward identity and let the module derive `UserId`, mirroring the module controller.
2. **Kit detail is admin-unscoped (IDOR risk).** `GetCargoDryKitDetail` (`GetByIdAsync`, "for admin inspection") has
   **no owner check** and is wired only into `CargoDryAdminController`. The owner detail screen must be **owner-gated at
   the BFF** — resolve the caller's kit set via `GetMyKits` and only return detail for a kit id in that set (a clean
   "not found" otherwise), exactly the pattern `GetMobileVesselDetailQueryHandler` already uses for vessels. Do **not**
   expose the admin unscoped detail to owners.

## BFF wiring prerequisite
Adding a new module behind the mobile BFF requires an **`oidc-audience-mapper` for the `cargodry-api` id** on the
Marine.Participant.Mobile BFF client, or every CargoDry call returns 401 → the BFF surfaces 500/911
([[provider_bff_module_audience_mapper]]). Restart the BFF after adding it to bust the cached service token. Typed
Refit bodies only — concrete DTOs, never `object`/`JsonElement` ([[provider_bff_typed_body_required]]).

---

## Phase MO11a — Mobile BFF CargoDry surface: validate + activate + my kits *(this kickoff → `BE_MO11a_CARGODRY_BFF_SURFACE.md`)*
Add an `ICargoDryRemoteCall` (Refit) + a `CargoDry` feature folder to the mobile BFF with three vertical slices:
- **ValidateMobileKit** (command → `public/validate`, anonymous downstream but the mobile endpoint stays behind normal
  mobile auth so only logged-in owners scan) → returns a mobile validation contract (IsValid/InvalidReason/ProductName/
  ValidityDays/HasSmartDevice/ActivationToken/TokenExpiresAt).
- **ActivateMobileKit** (command → `kits/activate`, forwards owner identity + `{ ActivationToken, VesselId, Method }`)
  → returns the activated kit contract. **Guard:** the `VesselId` must belong to the caller (reuse the vessel
  ownership gate / owned-set check) so an owner can't activate onto someone else's vessel.
- **GetMobileMyKits** (query → `kits`) → `{ Items, Total, ActiveCount, ExpiringCount }` mapped to a mobile contract.
- Register `cargodry-api` audience mapper; add BFF controller `CargoDryController` (mobile routes); unit tests for the
  three handlers + the vessel-ownership guard on activate.

## Phase MO11b — Owner-scoped kit detail + expiry/renewal read *(→ `BE_MO11b_CARGODRY_KIT_DETAIL.md`)*
- **GetMobileMyKitDetail** — owner-gated: resolve the caller's kit set (from `GetMyKits`), return detail only for an
  owned kit id, else "not found" (mirror `GetMobileVesselDetail`). Surface `EfficiencyPercent`, `DaysUntilExpiry`,
  `ExpiresAt`, `RenewalCount`, smart-device flag, vessel name (BFF-enriched).
- **Renewal reminder (read-only for MVP):** expose the kit's expiry/renewal status so the FE can show an "expiring in N
  days" banner. Actual renewal **purchase** is provider/admin-automated (`PrepareCargoDryKitRenewal`, renewal invoice/
  notification) and owner-initiated checkout is **deferred post-MVP** (no owner payment surface in scope). If a
  lightweight owner "remind/request renewal" is wanted, it's a notification-only action, not a payment.
- Tie the **expiry/renewal push** into N-F: an `EXPIRING`/renewal reminder notification deep-links to the kit detail
  (reuse the MO9d tap-deeplink + FE_NF3b routing).

## Phase MO11c — FE (RN/Expo): CargoDry tab *(→ `FE_MO11c_CARGODRY_OWNER.md`)*
- **CargoDry tab**: My Kits list (active/expiring badges from `ActiveCount`/`ExpiringCount`), empty state with a scan CTA.
- **Scan/validate**: camera QR scan → `validate` → show product + validity; if invalid, the reason.
- **Activate**: vessel picker (owner's vessels) → `activate` with the 5-min token → success → kit appears in My Kits.
- **Kit detail**: efficiency %, days-until-expiry, expiry date, renewal count, smart-device indicator; expiring banner.
- Deep-link target for the renewal/expiry push.

## Out of scope (post-MVP, note explicitly)
Owner-initiated renewal **checkout/payment**, kit **transfer** between owners (`TransferKit`), and any smart-device/
telemetry live feed. These are deferred; MO11 is activate + view + expiry-awareness.

## Sequencing
MO11a (BFF validate/activate/list) → MO11b (owner-gated detail + expiry read) → MO11c (FE). Each phase is
independently live-verifiable. **Do not commit.**
