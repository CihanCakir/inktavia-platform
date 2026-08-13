# BE_MO11b — Mobile BFF owner-gated kit detail + expiry/renewal read

> **Repo:** `addesso-project` — `Bff/src/Marine.Participant.Mobile`. Give the owner a **kit detail** view (efficiency /
> expiry / renewal count / smart-device) that is **owner-scoped**, plus an optional activation/renewal **timeline**.
> Builds on MO11a. Additive; owner surfaces are cost-free. **Do not commit.**

## Ownership rule (do NOT use the admin detail endpoint)
`GetCargoDryKitDetail` is **admin-only, owner-unscoped** (`GetByIdAsync`, wired into `CargoDryAdminController`), and
`CargoDryKitDetailDto` exposes **admin-sensitive** fields (`QrPayload`, `ConsignmentAgreementId`, `RevokeReason`,
audit timestamps). **Do not** expose it to owners — that would be an IDOR + data leak.

Instead, serve owner detail from the **owner's own kit set**, mirroring `GetMobileVesselDetailQueryHandler`:
1. Resolve the caller (asserted owner identity, same forward-only rule as MO11a — CargoDry keys on the Identity user id).
2. Call `GetMyKits` (already owner-filtered by `OwnerUserId`) and **find the requested kit id in that set**.
3. If not present → clean `AizenBusinessException("Kit not found.")` (never another owner's kit, never a 500).
4. Return a curated `MobileKitDetailDto` built from the owned `CargoDryKitDto` — **no admin fields**.

This needs **no new module endpoint** for the core detail: the `GetMyKits` item already carries `EfficiencyPercent`,
`DaysUntilExpiry`, `ExpiresAt`, `RenewalCount`, `Status`, `VesselId`, `ActivatedAt`, `ProductName`. The detail screen is
the owned list-item enriched.

## 1. `Query/GetMobileMyKitDetail`
- Input `{ KitId }`. Validator: `KitId > 0`.
- Handler: resolve identity → `GetMyKits` → first-or-not-found by `KitId` → map to `MobileKitDetailDto`.
- **Enrich (BFF layer):**
  - `VesselName` — if the kit has a `VesselId`, resolve from the caller's owned-vessel set (reuse
    `IVesselRemoteCall.GetUserVessels(0,20)`, the same owned-set the MO11a activate-guard uses); null if not found.
  - `HasSmartDevice` — not on `CargoDryKitDto`; if the FE needs it, carry it forward from the product (the
    `ValidateKit` DTO exposes it) or add it to the mobile mapping only if cheaply available. Otherwise omit for MVP.

## 2. Expiry / renewal — read-only for MVP
- The detail (and the MyKits list) already expose `DaysUntilExpiry` + `ExpiresAt` + `RenewalCount` → the FE renders an
  **"expiring in N days"** banner with **no extra call**. Surface an `IsExpiringSoon` convenience flag on the detail DTO
  (`Status == Activated && DaysUntilExpiry <= 30`) mirroring the module's `ExpiringCount` threshold, so the FE doesn't
  re-derive the rule.
- **Renewal purchase/checkout is OUT of scope** (owner has no payment surface in MVP; renewal is provider/admin
  automated — `PrepareCargoDryKitRenewal`, renewal invoice/notification). Do **not** add an owner renewal-pay command.
  If a lightweight owner "remind me / request renewal" is wanted later, it is a **notification-only** action, specced
  separately.

## 3. Optional — activation/renewal timeline
If the detail screen wants a history strip (activated, renewed, transferred events):
- Add `Query/GetMobileMyKitLifecycle` → calls the module `GetCargoDryKitLifecycleHistory` (or
  `GetCargoDryKitLifecycleEventsPaged`) for the kit — **but only after** the same owned-kit gate passes (resolve →
  confirm the kit id is in the owner's `GetMyKits` set). Map to a compact `MobileKitLifecycleDto[]` (event type, at,
  note); drop any admin-only fields.
- If this adds cost/complexity, defer it to MO11c polish — the core detail (efficiency/expiry/renewal count) is enough
  for the owner MVP.

## 4. Controller + contracts
- `CargoDryController`: `GET kits/{kitId}` → `GetMobileMyKitDetailQuery`; (optional) `GET kits/{kitId}/lifecycle` →
  `GetMobileMyKitLifecycleQuery`.
- Contracts: `MobileKitDetailDto` (curated superset of `MobileKitDto` + `VesselName`, `IsExpiringSoon`), optional
  `MobileKitLifecycleDto`. Extend `MobileCargoDryMapper`.

## Deep-link target
The expiry/renewal reminder push (N-F track) should deep-link to **this kit detail** (`ReferenceType=CargoDryKit`,
`ReferenceId=kitId`), reusing the MO9d tap-routing + FE_NF3b deeplink pattern. Confirm the mobile push-tap handler
routes a `CargoDryKit` ref to the kit-detail screen (FE lands in MO11c).

## Don't-break / QA
- Additive BFF only: one (optionally two) query slice(s) + contracts + controller routes. No module change, no other
  BFF feature touched. The admin detail endpoint is **not** exposed. Typed bodies; UTC pass-through.
- **Unit tests:** (1) detail for an **owned** kit returns curated dto (no admin fields) with correct
  efficiency/expiry/renewal; (2) detail for a **non-owned** kit id → "Kit not found", module detail **never** called;
  (3) `IsExpiringSoon` true at `DaysUntilExpiry ≤ 30 && Activated`, false otherwise; (4) vessel-name enrich resolves for
  an owned vessel, null otherwise; (5) (if built) lifecycle gated by the same owned-kit check.
- **Build:** mobile BFF solution 0 errors; test project green.

## Live verification
As the owner (`qa.owner.aug5@inktavia.com`): `GET /api/v1/mobile/cargodry/kits/{id}` for the kit activated in MO11a →
returns efficiency/expiry/renewal + vessel name, `IsExpiringSoon` correct. Request a **non-owned** kit id → "Kit not
found" (400/404 via BFF), the admin path never hit.

## Report
`docs/V1.0.1/Mobile/Owner/REPORT_BE_MO11b.md`: the owner-gated detail handler (owned-set gate, curated dto, enrich),
the expiry read + `IsExpiringSoon`, any lifecycle slice, the non-owned "not found" proof, and build/test results. Then
MO11c (RN/Expo CargoDry tab: scan → activate → my kits → detail). Cross-link [[mo11_cargodry_owner_progress]].
