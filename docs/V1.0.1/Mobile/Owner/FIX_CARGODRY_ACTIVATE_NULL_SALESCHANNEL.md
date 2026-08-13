# FIX — CargoDry activate NRE on `SalesChannel = null` kits (surfaced by MO11a)

> **Repo:** `addesso-project` (CargoDry module). Activating a kit whose `SalesChannel` is `null` (dev-seeded platform
> kits like `CDK-STAN-0001`) crashes the module handler with a null-dereference. The MO11a BFF already surfaces it
> cleanly as *"Kit activation failed."*, but the **module** should never NRE and the dev seed should let platform kits
> activate. Two grounded fixes; **no invented commercial policy**. **Do not commit.**

## Root cause (from code)
`CargoDryKitEntity.Activate(userId, vesselId, validityDays)` has a documented **N18 early-return**:
```csharp
// Decision N18/N19: if SalesChannel is not set, mark for commercial review
// and do NOT complete activation — caller must handle this return path.
if (SalesChannel == null)
{
    Status = CargoDryKitStatus.CommercialReviewRequired;
    return;                      // ← ActivatedAt / ExpiresAt stay NULL
}
```
`ActivateKitCommandHandler` calls `kit.Activate(...)` but **does not handle that return path** — it proceeds to publish
`CargoDryKitActivatedMessage` with:
```csharp
ActivatedAt = kit.ActivatedAt!.Value,   // null-forgiving on a genuinely null value → throws
ExpiresAt   = kit.ExpiresAt!.Value,
```
So a null-`SalesChannel` kit → `Status = CommercialReviewRequired`, `ActivatedAt/ExpiresAt = null` →
`Nullable.Value` throws. The domain explicitly said *"caller must handle this return path"* and the handler doesn't —
**that missing guard is the bug.**

## Fix 1 (required) — handler must honor the N18 return path, never NRE
In `ActivateKitCommandHandler`, immediately **after** `kit.Activate(...)` and **before** any lifecycle write / Mongo log
/ message publish / DTO map:
- If `kit.Status == CargoDryKitStatus.CommercialReviewRequired` (equivalently: the kit did **not** reach `Activated`),
  **stop** and throw a clean domain error, e.g.
  `throw new AizenBusinessException("SR_CARGODRY_KIT_COMMERCIAL_REVIEW_REQUIRED");`
  (a message the mobile layer already turns into a friendly "this kit isn't ready to activate yet").
- Do **not** publish `CargoDryKitActivatedMessage`, do **not** write the "Activated" lifecycle event / Mongo log, and
  do **not** dereference `ActivatedAt/ExpiresAt` on that path.
- Guard the dereference defensively too: the activated-path publish should only run when `ActivatedAt.HasValue &&
  ExpiresAt.HasValue` (belt-and-suspenders; after the status check they will be set, but never `!.Value` on a nullable
  that a domain branch can leave null).

This is a pure correctness fix — it does **not** decide what commercial policy a null-`SalesChannel` kit should get
(that stays Decision N18's `CommercialReviewRequired`); it only stops the crash and returns a clean, idempotent-safe
business error. The kit is left in `CommercialReviewRequired` (its persisted state from `Activate`), consistent with the
domain intent.

## Fix 2 (dev seed) — make platform kits activatable for the MO11 demo
Dev-seeded standalone/platform kits (`CDK-STAN-*`) currently seed with `SalesChannel = null`, so they can only ever hit
the review path — the owner can't demo activation on default seed. The entity already exposes the direct-platform path
(`SetForDirectPlatformSale()` / `MarkDirectPlatformSale`, which sets `SalesChannel = DirectSale`). In the **CargoDry dev
seeder** only:
- Seed the demo platform kits with `SalesChannel = DirectSale` (via the existing entity method, not a raw field poke),
  so a scanned platform kit activates end-to-end.
- **Idempotent / duplicate-safe** (match the existing seed guards — don't double-apply on re-seed).
- Dev/demo seed only; do **not** change production commercial attribution or invent a `CommercialModel`.

> If you'd rather keep some kits in the review state on purpose (to exercise the N18 path in the FE), seed a **mix**: a
> few `DirectSale` (activatable) + one left `null` (returns the clean review error) — both now behave correctly.

## Don't-break / QA
- Fix 1 is additive-guard only in the handler; the domain `Activate` is unchanged (its N18 branch is correct — the
  handler was the offender). No engine/economics/event-contract change for the normal activated path.
- Fix 2 is dev-seed only, idempotent, no production data.
- **Tests:** (1) unit — `Activate` on a `SalesChannel==null` kit leaves it `CommercialReviewRequired` with null
  Activated/Expires (already true) **and** the handler throws `SR_CARGODRY_KIT_COMMERCIAL_REVIEW_REQUIRED` instead of
  NRE, publishing **nothing**; (2) unit — a `DirectSale` kit activates fully (Activated, timestamps set, message
  published once); (3) seed test — demo platform kits are `DirectSale` and re-seed is idempotent; (4) `dotnet build`
  0 errors + CargoDry module tests green.
- **Live:** re-scan `CDK-STAN-0001` (now `DirectSale` after re-seed) as the owner → activates → appears in GetMyKits;
  a still-null kit → clean review error (HTTP 400 via the BFF), no 500, kit stays `CommercialReviewRequired`.

## Report
`docs/V1.0.1/Mobile/Owner/REPORT_FIX_CARGODRY_ACTIVATE_NULL_SALESCHANNEL.md`: the handler guard diff, the seed change,
the before/after (NRE → clean error / activatable platform kit), and test results. Cross-link
[[mo11_cargodry_owner_progress]]. This unblocks activating platform-seeded kits for the MO11 demo.
