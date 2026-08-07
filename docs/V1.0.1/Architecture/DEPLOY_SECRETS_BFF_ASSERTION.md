# Deploy note — required per-host secret: `BffAssertion:SharedSecret`

> Operator/deploy reference for HARDENING_GENERIC_CRUD_AND_SERVICE_TOKEN. The internal BFF→module identity
> assertion is now **enforced fail-closed outside Development**. **No secret value lives in the repo** — it is
> supplied from env/k8s secrets.

## What changed

Every host that calls `AddAizenInfoAccessor` (all module APIs + all BFFs + worker/scheduler/operation/gateway
starters) now **refuses to start outside Development unless `BffAssertion:SharedSecret` is non-empty.** In
Development an empty secret still means "assertion feature disabled" — local runs are unchanged.

Environment is read from `ASPNETCORE_ENVIRONMENT` (fallback `DOTNET_ENVIRONMENT`), defaulting to **Production**
(fail-closed) when unset.

## Required configuration per host (non-Development)

| Key | Env var (double-underscore) | Value | Notes |
|-----|------------------------------|-------|-------|
| `BffAssertion:SharedSecret` | `BffAssertion__SharedSecret` | strong random secret, **shared by all hosts in the mesh** | from k8s/CI secret, e.g. `AIZEN_BFF_ASSERTION_SECRET`. Empty ⇒ non-dev startup fails. |
| `BffAssertion:AllowedClientIds:N` | `BffAssertion__AllowedClientIds__0`, `__1`, … | the Keycloak `azp` client-ids of the trusted BFFs allowed to assert a user id (e.g. `provider-portal-bff`, `admin-panel-bff`, `marine-mobile-bff`) | **must be non-empty when the secret is set** (empty allowlist ⇒ startup fails — impersonation guard). |

Both sides of an internal call need the **same** `SharedSecret`: the BFF presents it in `X-Aizen-Bff-Assertion`,
the module compares it (fixed-time) before honoring `X-Aizen-User-Id` / `X-Aizen-Provider-Profile-Id`.

## Wiring

- **docker-compose** already wires `BffAssertion__SharedSecret: ${AIZEN_BFF_ASSERTION_SECRET:-}` and the per-host
  `BffAssertion__AllowedClientIds__*` for every service. Compose defaults `ASPNETCORE_ENVIRONMENT=Development`, so
  local runs boot with an empty secret. For a non-dev compose run, export a non-empty `AIZEN_BFF_ASSERTION_SECRET`
  (and set `ASPNETCORE_ENVIRONMENT`).
- **k8s**: mount `AIZEN_BFF_ASSERTION_SECRET` from a `Secret` into `BffAssertion__SharedSecret` on **every** module
  + BFF Deployment; set `BffAssertion__AllowedClientIds__*` per host. Do not commit the value.

## Generic entity controllers (also hardened here)

The generic per-entity controllers are **off by default** (fail-closed) and require `[Authorize]` when enabled — do
**not** enable them unless a specific authenticated need exists:

| Flag (env var) | Host | Default | Effect when `true` |
|----------------|------|---------|--------------------|
| `GenericBffCrud__Enabled` | BFF hosts | `false` | registers `AizenGenericBffApi<T>` (authenticated full CRUD per entity) |
| `GenericEntityApi__Enabled` | module API hosts | `false` | registers `AizenGenericApi<T>` (authenticated reads per entity) |
