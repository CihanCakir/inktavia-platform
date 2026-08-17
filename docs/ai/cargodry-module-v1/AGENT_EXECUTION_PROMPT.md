# Agent Execution Prompt — CargoDry Module v1

## Context

CargoDry, Inktavia Marine OS'un **primary user acquisition channel**'ıdır.
Fiziksel nem koruma kitleri QR kod ile platformun onboarding flow'unu tetikler.
Bu nedenle güvenlik katmanı (HMAC imzalama, batch key rotation, activation token) kritik öneme sahiptir.

Module host project: `Aizen.Modules.CargoDry`
6 proje, hepsi Class1.cs placeholder. Tüm Class1.cs dosyaları silinecek.

```
Modules/CargoDry/src/
├── Aizen.Modules.CargoDry                    ← ASP.NET Core host (Program.cs, controllers)
├── Aizen.Modules.CargoDry.Abstraction        ← Enums, DTOs, interfaces, message contracts
├── Aizen.Modules.CargoDry.Application        ← CQRS handlers, services, jobs, consumers
├── Aizen.Modules.CargoDry.Core               ← (reserved)
├── Aizen.Modules.CargoDry.Domain             ← 5 entities
└── Aizen.Modules.CargoDry.Repository         ← EF Core, DbContext, repos, seed
```

---

## Execution Order

**CRITICAL:** Execute phases in order. Do not skip. Each builds on previous.

---

### Phase 1 — Foundation  →  `PROMPT_A_CARGODRY_DOMAIN.md`

**Step 1.1** — Delete `Class1.cs` from ALL 6 projects.

**Step 1.2** — Implement `Abstraction` project:
- Enums: `CargoDryKitStatus` (7 values), `ActivationMethod`, `ActivationSource`, `RenewalType`
- DTOs: `CargoDryProductDto`, `CargoDryKitDto`, `CargoDryKitValidationDto`, `CargoDryStatsDto`, `GenerateBatchResultDto`
- Integration messages (5): `CargoDryKitActivatedMessage`, `CargoDryKitExpiringMessage`, `CargoDryKitExpiredMessage`, `CargoDryKitRenewedMessage`, `CargoDryKitRevokedMessage`
- Repository interfaces: `ICargoDryProductRepository`, `ICargoDryKitRepository`, `ICargoDryBatchRepository`
- Service interfaces: `ICargoDryQrService`, `IActivationTokenService`, `IBatchKeyVaultService`

**Step 1.3** — Implement `Domain` project (5 entities):
- `CargoDryProductEntity` — Create, SetActive, UpdatePrice
- `CargoDryBatchEntity` — Create, Revoke, SetFileRefs
- `CargoDryKitEntity` — Create, Activate, Renew, MarkExpired, Revoke, Transfer
  - `EfficiencyPercent` → NOT MAPPED (EF Ignore)
  - `DaysUntilExpiry` → NOT MAPPED (EF Ignore)
- `CargoDryActivationLogEntity` — Create
- `CargoDryRenewalEntity` — Create

**Step 1.4** — Implement `Repository` project:
- `CargoDryDbContext` (schema: `cargodry`)
- 5 EF Core configs (tables: `products`, `batches`, `kits`, `activation_logs`, `renewals`)
- 3 repository implementations
- `CargoDryProductSeed` — 4 products (STANDARD-90, PREMIUM-180, PREMIUM-365, SMART-90), idempotent
- `DependencyInjection.cs` with `AddCargoDryRepository()` and `SeedCargoDryAsync()`

**Verify:** `dotnet build` all 3 projects — zero errors.

---

### Phase 2 — Application  →  `PROMPT_B_CARGODRY_APPLICATION.md`

**Step 2.1** — Security services:
- `CargoDryQrService` — Base32 serial number (80-bit entropy), HMAC-SHA256 sign/verify with `FixedTimeEquals`, QR PNG generation
- `ActivationTokenService` — 5-min JWT, JTI stored in Redis, Lua script for atomic consume
- `BatchKeyVaultService` — appsettings fallback for MVP, Key Vault interface for production

**Step 2.2** — Commands:
- `ValidateKitCommand` + Handler — 6-layer validation chain (batch revoked → HMAC sig → kit exists → Available → product active → issue token)
- `ActivateKitCommand` + Handler — consume JTI, `kit.Activate()`, publish `CargoDryKitActivatedMessage`
- `GenerateBatchCommand` + Handler — create batch, generate N kits with serial+QR+sig, bulk insert
- `RevokeKitCommand` + Handler — `kit.Revoke()`, publish `CargoDryKitRevokedMessage`
- `RenewKitCommand` + Handler — `kit.Renew()`, create `CargoDryRenewalEntity`, publish `CargoDryKitRenewedMessage`

**Step 2.3** — Queries:
- `GetMyKitsQuery` + Handler
- `GetAdminKitListQuery` + Handler (paged, filterable)
- `GetCargoDryStatsQuery` + Handler

**Step 2.4** — Jobs:
- `KitExpiryReminderJob` (BackgroundService) — daily at 09:00 UTC, publishes `CargoDryKitExpiringMessage` at 30/7/1 days
- `KitExpiredMarkingJob` (BackgroundService) — hourly, marks Activated+expired kits to Expired, publishes `CargoDryKitExpiredMessage`

**Step 2.5** — Consumer:
- `CommerceOrderCompletedConsumer` — listens for Commerce order completions, triggers `RenewKitCommand` for CargoDry renewal items

**Step 2.6** — `DependencyInjection.cs` with all service registrations + BackgroundService registrations.

**Verify:** `dotnet build Aizen.Modules.CargoDry.Application` — zero errors.

---

### Phase 3 — API Host  →  `PROMPT_C_CARGODRY_API.md`

**Step 3.1** — Replace `Program.cs` (weather forecast template → full AizenApplicationBuilder):
```
AizenApplicationBuilder.CreateBuilder(new AizenAppInfo {
    Name = "CargoDry",
    Type = AppType.Operation,
    TypeInclude = { AppType.Api, AppType.Worker, AppType.Scheduler }
})
```
Register: DbContext, Repository, Application, MediatR, Redis, RateLimiter, MassTransit (1 consumer).
Map: `MapControllers()`.

**Step 3.2** — Controllers:
- `CargoDryPublicController` at `/api/v1/cargodry/public`
  - `POST /validate` → `[AllowAnonymous]` + `[EnableRateLimiting("validate-ip")]` → 10 req/min/IP
- `CargoDryKitsController` at `/api/v1/cargodry/kits`
  - `GET /` → GetMyKitsQuery (owner's kits)
  - `POST /activate` → ActivateKitCommand
- `CargoDryAdminController` at `/api/v1/cargodry/admin`
  - `GET /kits` → paged list
  - `GET /stats` → stats
  - `POST /batches/generate` → GenerateBatchCommand
  - `POST /kits/{id}/revoke` → RevokeKitCommand
  - `POST /kits/{id}/extend` → RenewKitCommand (AdminExtension)

**Step 3.3** — `appsettings.json` CargoDry, Redis, RabbitMq sections added.

**Verify:**
```bash
dotnet build Aizen.Modules.CargoDry
dotnet run --project Aizen.Modules.CargoDry
# POST /api/v1/cargodry/public/validate (no auth) → 400/200
# GET  /api/v1/cargodry/admin/stats (with admin JWT) → 200
```

---

### Phase 4 — BFF Integration  →  `PROMPT_D_CARGODRY_BFF.md`

**Step 4.1** — Update `CargoDryKitBffDto` — add all fields (SerialNumber, BatchCode, VesselId, EfficiencyPercent, DaysUntilExpiry, RenewalCount, ManufacturedAt, OwnerDisplayName, VesselName).

**Step 4.2** — Add `IAdminCargoDryBffRemoteCall` (Refit) with 7 methods.

**Step 4.3** — Create `AdminCargoDryController` at `/api/v1/admin-panel/cargodry` (kits, stats, batch generate, revoke, extend).

**Step 4.4** — Create `CargoDryOnboardingController` at `/api/v1/onboarding/cargodry` (validate=AllowAnonymous, activate=Authorize).

**Step 4.5** — Update `GetAdminVesselDetailBffQueryHandler` — replace "not yet integrated" comment with real Refit call.

**Step 4.6** — Remove/replace CargoDry 501 stubs in `AdminInactiveModulesController`.

**Step 4.7** — Register Refit client in BFF Program.cs: `Services:CargoDry` base URL.

**Verify:**
```bash
# Was 501, now 200:
GET /api/v1/admin-panel/cargodry/kits
# Was missing, now works:
POST /api/v1/onboarding/cargodry/validate (no auth)
```

---

### Phase 5 — Frontend  →  `PROMPT_E_CARGODRY_FRONTEND.md`

**Step 5.1** — Complete rewrite `cargodry.types.ts` (delete old product catalog types, write kit management types).

**Step 5.2** — Update `cargodryApi.ts` (correct endpoints, all methods).

**Step 5.3** — Create hooks: `useCargoDryKitsQuery`, `useCargoDryStatsQuery`, `useRevokeKit`, `useExtendKit`, `useGenerateBatch`.

**Step 5.4** — Create components: `CargoDryStatsBar`, `KitStatusBadge`, `CargoDryKitDetailPanel`, `CargoDryBatchGenerateModal`.

**Step 5.5** — Complete rewrite `CargoDryListPage.tsx` with stats bar, filter tabs, search, table with 8 columns, pagination, slide-over panel.

**Step 5.6** — Verify `CargoDryKitsCard.tsx` prop interface matches (efficiency, daysLeft).

**Verify:**
```bash
npx tsc --noEmit 2>&1 | grep -E "(cargodry|CargoDry)"
# Expected: no output
```

---

### Phase 6 — Migration + Smoke Test

```bash
# Migration
dotnet ef migrations add InitialCreate \
  --project Aizen.Modules.CargoDry.Repository \
  --startup-project Aizen.Modules.CargoDry \
  --context CargoDryDbContext \
  --output-dir Persistence/Migrations

# Run
dotnet run --project Aizen.Modules.CargoDry

# Verify:
# 1. cargodry schema created in PostgreSQL
# 2. 4 product seeds: SELECT * FROM cargodry.products;
# 3. POST /api/v1/cargodry/public/validate with fake data → IsValid: false (not 500)
# 4. POST /api/v1/cargodry/admin/batches/generate → batch + kits created
# 5. KitExpiredMarkingJob starts without error
# 6. MassTransit consumer registered
```

---

## Key Constraints

| Constraint | Detail |
|---|---|
| HMAC signing | `HMAC_SHA256(batchSecretKey, serial + ":" + batchCode)` — 16-char signature |
| Timing attack protection | `CryptographicOperations.FixedTimeEquals()` in verify |
| Activation token | 5-min JWT, JTI (UUID v4), Redis Lua atomic consume |
| Serial number | Base32 (no 0,1,O,I), 80-bit entropy, XXXX-XXXX-XXXX-XXXX format |
| Rate limiting | 10 req/min/IP on `/validate`, Redis sliding window (6 segments) |
| Batch key storage | appsettings in dev, Azure Key Vault in prod (per-batch, rotatable) |
| Duplicate vessel kit | Same vessel + same product → old kit `MarkExpired()` before new activation |
| EF NOT MAPPED | `EfficiencyPercent` and `DaysUntilExpiry` are computed → `builder.Ignore()` |
| Single process | Operation status — Api + Worker + Scheduler in one host |
| Event-only | Other modules reach CargoDry ONLY via MassTransit. No direct HTTP module calls. |
| Public validate | `[AllowAnonymous]` — no auth token needed. QR scan works pre-login. |
| Onboarding token persistence | Frontend sessionStorage (5-min window). Never sent to server until activate. |
