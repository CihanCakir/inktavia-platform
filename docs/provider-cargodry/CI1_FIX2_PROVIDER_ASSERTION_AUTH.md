# CI-1 FIX 2 — wire the CargoDry module into the provider-BFF assertion trust

After the DI fix, `GET /provider/cargodry/overview` now fails with **401 from the module** (not the BFF):
```
Response status code does not indicate success: 401 (Unauthorized).
  at IProviderCargoDryRemoteCall.GetOverview()
```
The BFF's `MarineProviderBffAuthDelegatingHandler` sends the service token **and** the trusted-BFF assertion headers
(`X-Aizen-Bff-Assertion`, `X-Aizen-User-Id`, `X-Aizen-Provider-Profile-Id`). The CargoDry module **rejects them** because
it was **admin-only** and never wired into the provider-BFF assertion trust that ServiceRequest/Vessel have. Two gaps
(diff against the working `service-request-api`):

1. **Module code:** `Modules/CargoDry/.../Program.cs` does **not** call `AddAizenInfoAccessor(...)` (ServiceRequest's
   `Program.cs` does, line ~31, with `using Aizen.Core.InfoAccessor.Extensions;`). That registration is what reads the
   BFF assertion → `KeycloakTokenInfo.ProviderProfileId` and lets the module trust the BFF-asserted identity.
2. **Compose:** `cargodry-api` is **missing** the `BffAssertion` config that `service-request-api` has:
   ```
   BffAssertion__SharedSecret: ${AIZEN_BFF_ASSERTION_SECRET:-}
   BffAssertion__AllowedClientIds__0: provider-portal-bff
   ```
   Without it the module doesn't trust the BFF assertion and falls back to strict token-audience validation — the
   provider-BFF service token (aud `provider-portal-bff`) ≠ `cargodry-api` → 401.

## Fix — mirror `service-request-api` exactly
1. **`Modules/CargoDry/src/Aizen.Modules.CargoDry/Program.cs`**: add `AddAizenInfoAccessor(builder.Configuration)` (and
   any companion auth/assertion registration ServiceRequest has that CargoDry lacks — diff the two `Program.cs` auth
   sections and bring CargoDry to parity for the inbound provider-assertion path). Ensure it composes with CargoDry's
   existing (admin) auth rather than replacing it — admin endpoints must keep working.
2. **`docker-compose.yaml` → `cargodry-api` environment**: add
   `BffAssertion__SharedSecret: ${AIZEN_BFF_ASSERTION_SECRET:-}` and
   `BffAssertion__AllowedClientIds__0: provider-portal-bff` (copy from the `service-request-api` block).
3. Rebuild + restart **cargodry-api** (module code + compose) and **bff-marineprovider**.

## Acceptance
- `GET /provider/cargodry/overview` (provider2) → **200** with the overview DTO (no 401).
- `GET /provider/cargodry/alerts?take=5` → 200.
- Admin CargoDry endpoints (AdminPanel BFF) still work (no auth regression).

## Report
Append to `REPORT_BACKEND.md` ("CI-1 fix 2"): CargoDry now accepts the provider-BFF assertion (AddAizenInfoAccessor +
BffAssertion config, mirroring ServiceRequest); overview/alerts return 200 for the provider.

## Note (already flagged) — provider scoping still deferred
The overview/alerts handlers accept `ProviderProfileId` but don't yet filter by it (they compute globally). Once auth
works, the numbers will be the GLOBAL totals until the handler-level filter is threaded — track that as the CI-1
follow-up before CI-2 (a provider must not see platform-wide totals).
