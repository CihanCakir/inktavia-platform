# CI-1 — Backend: Provider CargoDry BFF foundation + Overview + Alerts (provider-scoped)

Unblocks the CargoDry Komuta Merkezi page (FE-A KPI board + kit-status donut, FE-B critical alert banner). The CargoDry
module is mature but **admin-only** — its `Overview` / `Alerts` queries are **global** and there is **no provider BFF**.
This phase (1) makes those two queries **provider-scoped**, and (2) stands up the **provider CargoDry BFF surface**,
mirroring the Jobs/ServiceRequest provider pattern (identity via the BFF assertion, never client-sent).

## Verified in source (2026-07-17)
- `GetCargoDryOperationalOverviewQuery` = **empty (global)** → `CargoDryOperationalOverviewDto` (TotalKits, AvailableKits,
  ActivatedKits, ExpiredKits, RevokedKits, RenewalDueSoonKits (≤30d), ProviderHeldKits, WarehouseStockKits,
  RecentLifecycleEventCount, OpenOperationalAlertCount).
- `GetCargoDryOperationalAlertsQuery` = Page/PageSize only (global); handler computes from kit repo state
  (`_kits.GetExpiringAsync(30)`, `GetExpiredUnmarkedAsync()`, `GetPagedAsync(...)`). Alert DTO carries `ProviderProfileId,
  OwnerDisplayName, VesselName, ProductName, AlertType, Severity, Message, DaysUntilExpiry, ExpiresAt, KitStatus`.
- `CargoDryKitEntity` **has `ProviderProfileId (long?)`** → kits are provider-linkable (scope by it).
- Module controllers are all `api/v1/cargodry/admin/...` (`CargoDryAdminController` exposes
  `operational-overview`, `kits/operational-alerts`). The **provider inventory** list already accepts
  `[FromQuery] long? providerProfileId`, so the module has the filter precedent.
- Provider BFF remote calls exist for Identity/ServiceRequest/FileStorage/Vessel (`IProviderServiceRequestRemoteCall`
  etc.), each with a compose `RemoteCalls__I…RemoteCall__BaseUrl`. **No `IProviderCargoDryRemoteCall`.** The
  `ProviderJobsController` (BFF) + `ProviderProfileResolver`/`IProviderIdentityHolder` are the pattern to copy.
- Provider→module identity uses the **shared-secret BFF assertion** carrying `ProviderProfileId`, read in the module via
  `_info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId` (see `ServiceRequest` provider controllers) —
  CargoDry does **not** have this wiring yet; add it.

## Work

### 1. Module — make Overview + Alerts provider-scoped
- **`GetCargoDryOperationalOverviewQuery`**: add `long? ProviderProfileId { get; init; }`. In the handler, when set,
  scope every counter to the provider — filter kits by `ProviderProfileId == x` (and provider inventory rows for the
  stock counters). Add a `providerProfileId` filter to the kit/inventory repo count methods it uses (or filter the
  fetched set). Global behavior (null) unchanged for admin.
- **`GetCargoDryOperationalAlertsQuery`**: add `long? ProviderProfileId`. Thread it into the kit repo calls
  (`GetExpiringAsync`, `GetExpiredUnmarkedAsync`, `GetPagedAsync`) so only the provider's kits produce alerts. (If adding
  the filter to those repo methods is heavy, an acceptable interim is to filter the resulting alert list by
  `ProviderProfileId` before paging — but prefer pushing the filter into the query.)
- **Keep `OwnerDisplayName` / `VesselName`** on the alert DTO — product decision confirmed: the provider MAY see the
  vessel/owner of a kit they hold/sold (CargoDry consignment differs from anonymous SR discovery).

### 2. Module — provider-scoped CargoDry controller (assertion identity)
Add `ProviderCargoDryController` at `api/v1/cargodry/provider`, resolving the provider from the assertion
(`_info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0`; if `0` → `AizenBusinessException("Provider
identity could not be resolved.")`). Wire the CargoDry module into the provider-BFF assertion trust the SAME way
ServiceRequest is (audience/assertion validation — mirror that module's setup).
- `GET provider/overview` → `GetCargoDryOperationalOverviewQuery { ProviderProfileId = pid }`.
- `GET provider/alerts?page=&pageSize=` → `GetCargoDryOperationalAlertsQuery { ProviderProfileId = pid, Page, PageSize }`.

### 3. BFF (MarineProvider) — provider CargoDry surface
- `IProviderCargoDryRemoteCall` (Refit) → `[AizenRemoteCallGet("/api/v1/cargodry/provider/overview")]`,
  `[AizenRemoteCallGet("/api/v1/cargodry/provider/alerts")]`.
- `ProviderCargoDryController` (`api/v1/provider/cargodry`), provider-scoped (`ProviderProfileResolver` + holder, assertion),
  passthrough:
  - `GET /provider/cargodry/overview` → remote `overview`.
  - `GET /provider/cargodry/alerts?take=` → remote `alerts` (map `take` → pageSize; default 5 for the banner + a count).
- Register the remote call + add the compose env `RemoteCalls__IProviderCargoDryRemoteCall__BaseUrl:
  http://cargodry-api:8080`, and add `cargodry-api` to the BFF `depends_on` if not present.

## Constraints
- **Provider-scoped**: a provider sees only THEIR kits/inventory in the overview + alerts. Identity via the assertion,
  never a client-sent id. Admin/global behavior of the queries is preserved (null provider filter).
- Owner/vessel names **shown** (confirmed). No money on this surface. Counts are integers.
- Do NOT touch discovery/admin snapping or other modules.

## Acceptance — observed
- `GET /provider/cargodry/overview` (as provider2) → counters reflect **only provider2's** kits/inventory (not the global
  platform), e.g. AvailableKits/ActivatedKits/ExpiredKits/RenewalDueSoonKits for their stock.
- `GET /provider/cargodry/alerts` → only alerts for provider2's kits, each with `OwnerDisplayName/VesselName/Message/
  Severity`.
- A provider with no kits → zeroed overview + empty alerts (not the platform totals).

## Report
Append to `REPORT_BACKEND.md` ("CI-1 provider CargoDry overview/alerts"): the provider-scoped overview + alerts for
provider2, and confirmation the numbers differ from the global admin overview. Unfinished is **not done**.

## Frontend (I build in parallel — FE-A + FE-B)
FE-A: page in the provider shell + 5 KPI cards (Mevcut Stok gold-emphasized, Aktif Kitler, Yaklaşan Yenilemeler, Kritik
Uyarılar, İade/İptal) from `overview`, + the Kit Durumu donut (Aktif/Yenileme Yaklaşıyor/Gecikmiş/Kurulum Bekliyor from
the overview counters). FE-B: the critical alert banner from `alerts` (highest severity; real operational messages, no
telemetry). Inventory table + movements = CI-2; renewal calendar = CI-3.
