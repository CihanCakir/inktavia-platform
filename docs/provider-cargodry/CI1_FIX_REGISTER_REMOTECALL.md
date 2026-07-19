# CI-1 FIX — register `IProviderCargoDryRemoteCall` in DI

`GET /provider/cargodry/overview|alerts` returns **500**:
```
Unable to resolve service for type
'Aizen.Bff.MarineProvider.Application.Common.RemoteClients.IProviderCargoDryRemoteCall'
while attempting to activate 'ProviderCargoDryController'.
```
The Refit interface + controllers were created, and the compose base URL was added, but the **DI registration for the
remote call was missed** — so the controller can't resolve it.

## Fix (one registration, mirrors the siblings)
In `Bff/src/MarineProvider/Aizen.Bff.MarineProvider.Application/DependencyInjection.cs`, alongside the other provider
remote calls (`IProviderServiceRequestRemoteCall`, `IProviderVesselRemoteCall`, …), add — before `return services;`:

```csharp
services.AddTransient<IProviderCargoDryRemoteCall>(provider =>
    CreateRemoteCall<IProviderCargoDryRemoteCall>(
        CreateHttpClient(provider, nameof(IProviderCargoDryRemoteCall))));
```

Add the `using` for the `IProviderCargoDryRemoteCall` namespace if needed. `nameof(IProviderCargoDryRemoteCall)` must
match the compose key already present (`RemoteCalls__IProviderCargoDryRemoteCall__BaseUrl: http://cargodry-api:8080`).

## Acceptance
- `GET /provider/cargodry/overview` → 200 with the overview DTO (no DI resolution error).
- `GET /provider/cargodry/alerts?take=5` → 200.

## Report
One line in `REPORT_BACKEND.md` ("CI-1 fix"): registered `IProviderCargoDryRemoteCall` in the BFF DI; overview/alerts
now 200.
