# Phase 1A — rename remote-calls `IProvider{X}RemoteCall` → `I{X}RemoteCall`

Mechanical rename, **no behaviour change**. This BFF is provider-only, so the `Provider` prefix on remote-call
interfaces is redundant. The HttpClient name is `nameof(interface)`, so the `RemoteCalls:*` config keys (appsettings
+ docker-compose) MUST move in lock-step or the BFF loses every downstream BaseUrl → 500s.

## The 7 renames (exact)
| Old | New |
|---|---|
| `IProviderIdentityRemoteCall` | `IIdentityRemoteCall` |
| `IProviderServiceRequestRemoteCall` | `IServiceRequestRemoteCall` |
| `IProviderFileStorageRemoteCall` | `IFileStorageRemoteCall` |
| `IProviderNotificationRemoteCall` | `INotificationRemoteCall` |
| `IProviderReferenceDataRemoteCall` | `IReferenceDataRemoteCall` |
| `IProviderVesselRemoteCall` | `IVesselRemoteCall` |
| `IProviderCargoDryRemoteCall` | `ICargoDryRemoteCall` |

(No name collisions — confirmed no existing `I{X}RemoteCall` without the `Provider` prefix in the BFF.)

## What to change
Do a scoped rename of each exact token `IProvider{X}RemoteCall` → `I{X}RemoteCall` across:

1. **Interface files** — `Bff/src/MarineProvider/Aizen.Bff.MarineProvider.Application/Common/RemoteClients/IProvider{X}RemoteCall.cs`:
   rename the **file** to `I{X}RemoteCall.cs` and the interface name inside. Keep namespace/methods identical.
2. **All `.cs` references** (54 files) — handlers, controllers, `Common/Services/ProviderProfileResolver.cs`,
   `Common/Authorization/ProviderAuthorization.cs`, and `DependencyInjection.cs`. In `DependencyInjection.cs` the
   `AddTransient<…>`, `CreateRemoteCall<…>` and `nameof(…)` all update automatically with the type name — just
   rename the type references. (The `nameof` now yields the new string; the config keys below must match it.)
3. **BFF appsettings** — in `Bff/src/MarineProvider/Aizen.Bff.MarineProvider/configuration/`:
   `appsettings.json`, `appsettings.Local.json`, `appsettings.Development.json`, `appsettings.Production.json` —
   rename the `RemoteCalls` child keys `IProvider{X}RemoteCall` → `I{X}RemoteCall`. (Ignore `bin/**` copies.)
4. **docker-compose.yaml** — rename all 10 keys (both bff-marineprovider instances, lines ~198-202 and ~264-268):
   `RemoteCalls__IProvider{X}RemoteCall__BaseUrl` → `RemoteCalls__I{X}RemoteCall__BaseUrl` for Identity,
   ServiceRequest, FileStorage, Vessel, CargoDry. (Notification + ReferenceData BaseUrls live only in appsettings —
   make sure those got renamed in step 3.)

Do NOT change: routes, `[Tags]`, method signatures, request/response payloads, or any module-side type. Only the
BFF interface name + its config keys.

## Acceptance
- Solution builds (0 errors). `grep -rn "IProvider.*RemoteCall" Bff/src/MarineProvider --include=*.cs` and the same
  over `configuration/*.json` + `docker-compose.yaml` return **nothing** (only the new `I{X}RemoteCall` names remain).
- After `docker compose build bff-marineprovider bff-marineprovider-2 && docker compose up -d …`: existing provider
  endpoints still return 200 — smoke `/provider/me/status`, `/provider/jobs/summary`, `/provider/cargodry/overview`,
  `/provider/service-requests/open`, `/provider/notifications` (i.e. at least one endpoint per renamed remote-call).
  A 500/911 with a missing-BaseUrl or DI-resolution error means a config key was missed.

## Report
Append to `REPORT_BACKEND.md` ("Phase 1A"): renamed the 7 provider remote-call interfaces (dropped `Provider`
prefix) across code + appsettings + docker-compose; HttpClient names + BaseUrl keys moved in lock-step; no behaviour
change; smoke green.
