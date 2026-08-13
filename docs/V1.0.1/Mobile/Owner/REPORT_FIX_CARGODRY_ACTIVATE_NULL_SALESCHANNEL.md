# REPORT — FIX: CargoDry activate NRE on `SalesChannel = null` kits

> **Repo:** `addesso-project` (CargoDry module). Surfaced by MO11a. **Not committed** (per instruction).
> Cross-link: [[mo11_cargodry_owner_progress]], MO11a report `REPORT_BE_MO11a.md`.

## Root cause (recap)
`CargoDryKitEntity.Activate()` takes a documented Decision-N18 early-return when `SalesChannel == null` — it sets
`Status = CommercialReviewRequired` and returns **without** setting `ActivatedAt`/`ExpiresAt`. `ActivateKitCommandHandler`
did not honor that return path: it proceeded to publish `CargoDryKitActivatedMessage` with `kit.ActivatedAt!.Value` /
`kit.ExpiresAt!.Value` → `System.InvalidOperationException: Nullable object must have a value` (HTTP 500, whole command
rolled back). The domain comment literally said *"caller must handle this return path"* — the missing handler guard was
the bug.

## Fix 1 (required) — handler honors the N18 return path, never NREs
`Modules/CargoDry/src/Aizen.Modules.CargoDry.Application/Commands/ActivateKit/ActivateKitCommandHandler.cs`

Immediately after `kit.Activate(...)`, before any attribution/save/lifecycle-write/Mongo-log/publish/DTO-map:
```csharp
kit.Activate(request.UserId, request.VesselId, product.ValidityDays);

// Decision N18/N19: a kit with no SalesChannel does NOT complete activation — Activate() marks it
// CommercialReviewRequired and returns early, leaving ActivatedAt/ExpiresAt null. Honor that documented
// return path here instead of proceeding to publish/log/map (which dereferenced the null timestamps and NRE'd).
if (kit.Status != CargoDryKitStatus.Activated)
    throw new AizenBusinessException("SR_CARGODRY_KIT_COMMERCIAL_REVIEW_REQUIRED");
```
Plus a belt-and-suspenders change at the publish (never `!.Value` a nullable a domain branch can leave null):
```csharp
ActivatedAt = kit.ActivatedAt ?? throw new AizenBusinessException("SR_CARGODRY_KIT_COMMERCIAL_REVIEW_REQUIRED"),
ExpiresAt   = kit.ExpiresAt   ?? throw new AizenBusinessException("SR_CARGODRY_KIT_COMMERCIAL_REVIEW_REQUIRED"),
```
`+using Aizen.Core.Infrastructure.Exception;` (already used across the module). Pure correctness fix — it does **not**
invent commercial policy; the kit stays in Decision-N18 `CommercialReviewRequired`, the command rolls back (nothing
published/logged), and the caller gets a clean, re-scannable business error instead of a 500.

## Fix 2 (dev seed) — make platform kits activatable for the MO11 demo
`Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository/Seed/CargoDryBatchMockSeed.cs`

The three **Available** demo kits (the only scannable ones) all seeded with no `SalesChannel`, so every owner scan hit the
review path. Made a **mix** (per the doc), using the existing entity method (not a raw field poke):
- `CDK-STAN-0001` (kit1): `+ kit1.MarkAsDirectSale();` → activatable.
- `CDK-PREM-0001` (kit5): `+ kit5.MarkAsDirectSale();` → activatable.
- `CDK-PREM-0004` (kit8): **left null on purpose** → exercises the N18 clean-review-error path.

Idempotent/duplicate-safe by the existing guard (`if (await _db.Kits.AnyAsync(ct)) return;`) — the seed only runs on an
empty kits table, and `MarkAsDirectSale()` is deterministic. Dev/demo only; no production attribution, no invented
`CommercialModel` (the method sets the entity's own `DirectSale` / `PrincipalSale`).

## Build & tests
- `dotnet build` CargoDry module (`Aizen.Modules.CargoDry`): **0 errors**.
- **Unit tests:** the CargoDry module has **no test project and no mocking library** (the handler has 9 dependencies);
  standing one up is out of proportion to this guard fix, so verification is by build + the live proofs below. Noted as a
  gap. The domain `Activate()` N18 branch was already correct and is unchanged.

## Live verification (docker stack, real Keycloak owner `qa.owner.aug5@inktavia.com`)
Rebuilt + recreated `cargodry-api` with both fixes. (The batch seed does not re-fire on the existing dev DB — kits already
present — so the running `CDK-STAN-0001` was still null-`SalesChannel`, ideal for the Fix-1 proof.)

**Fix 1 — null-`SalesChannel` kit no longer NREs.** `POST …/activate {CDK-STAN-0001 → vessel 100014}`:
- **Before:** `InvalidOperationException: Nullable object must have a value` → HTTP 500.
- **After:** module throws `AizenBusinessException: SR_CARGODRY_KIT_COMMERCIAL_REVIEW_REQUIRED` → **HTTP 400**, no
  `Nullable object…` in the logs, kit stays `Available` (clean rollback, re-scannable). ✓

**Fix 2 effect — a platform DirectSale kit activates end-to-end.** Simulated the re-seed outcome on the existing row
(`SalesChannel=DirectSale, CommercialModel=PrincipalSale` — exactly what `MarkAsDirectSale()` sets; a full destructive
re-seed was avoided to preserve existing demo data), then ran the real handler: `POST …/activate {CDK-STAN-0001 → vessel
100013}` → **HTTP 200**, kit `Activated`; `GET …/kits` → `total 2, active 2` (CDK-STAN-0001 + the MO11a-activated
CDK-PRV2-0001). ✓

Net: null-`SalesChannel` → clean 400 review error (no 500); `SalesChannel`-set platform kit → activates and appears in
GetMyKits. Unblocks the MO11 demo.

## Files changed (uncommitted)
- `Modules/CargoDry/src/Aizen.Modules.CargoDry.Application/Commands/ActivateKit/ActivateKitCommandHandler.cs` (Fix 1)
- `Modules/CargoDry/src/Aizen.Modules.CargoDry.Repository/Seed/CargoDryBatchMockSeed.cs` (Fix 2)
