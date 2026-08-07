# REPORT — HARDENING-2: lock down the anonymous generic-CRUD surface + enforce the internal service-token

> Executes `HARDENING_GENERIC_CRUD_AND_SERVICE_TOKEN.md`. Framework-wide, security-sensitive → diagnostic-first,
> decision, then a surgical **fail-closed** change. Second half of the go-live hardening (after the messagebus
> opt-out). **Additive/behaviour-preserving** for every authenticated flow; only the **anonymous** generic surface
> closes. **NOT committed**; secrets stay in env/k8s.

---

## Phase 0 — diagnostic (gated the decision)

**1. Who calls the generic BFF CRUD routes / the generic reads?**
- **FE repos** (`inktavia-marine-admin-web`, `inktavia-marine-provider-web`, `inktavia-marine-mobile`): grep for the
  generic route shape (`api/{Entity}`, `/search/list`, `/search/paged`, `AddEntity`/`UpdateEntity`/`DeleteEntity`,
  `AizenGeneric*`) → **zero matches**. Every FE call goes to a named BFF route, never the generic surface.
- **Server-side C#**: no caller of `AizenGenericBffApi` / `AizenGenericApi` / `IAizenGenericApi<T>` beyond the
  controller definitions + their auto-registration. (Hardening-1 already found no code publishes
  `AizenGenericMessage`; this HTTP surface is independently unused too.)
- **Conclusion: unused scaffolding** — both the BFF full-CRUD surface and the module read surface.

**2. Registration — auto or opt-in?** **Auto for every entity on every host.**
- BFF full-CRUD: `AizenBffServiceConfiguration.BffRestControllerFeatureProvider` closes `AizenGenericBffApi<T>` over
  every `AizenEntity` and adds it as a controller — on every BFF host.
- Module read: `Core.Api.AddAizenApi.GenericRestControllerFeatureProvider` does the same for `AizenGenericApi<T>` —
  on every module API host (Api/Operation/Gateway starters).

**3. `BffAssertion:SharedSecret` per env?** Wired in `docker-compose.yaml` from `${AIZEN_BFF_ASSERTION_SECRET:-}`
(empty default) for every module + BFF, with `BffAssertion__AllowedClientIds__*` set per host. Not in appsettings.
`ASPNETCORE_ENVIRONMENT` defaults to `Development` in compose. Existing startup validation: **secret-set +
empty-allowlist ⇒ fail** (impersonation guard); **empty-secret ⇒ feature disabled, boots anywhere** — i.e. **no
non-dev enforcement** existed.

**4. Internal module remote-call endpoints `[Authorize]`?** Yes — e.g. `PaymentInternalController`
(`ResolveLineCommissions`, …) is `[Authorize]` (valid JWT/service-token required, not Admin-restricted) and relies
on the assertion for identity. The anonymity was **only** on the generic controllers (their `[AllowAnonymous]`
overrode everything).

---

## Decision (gated on Phase 0)

Both generic surfaces are **unused scaffolding auto-exposed everywhere** → the safest, most surgical fix is
**disable by default + defense-in-depth auth**, never "leave on but authed":

- **`AizenGenericBffApi` (BFF full CRUD):** **not registered by default** (opt-in `GenericBffCrud:Enabled`, default
  off). And `[AllowAnonymous]` → `[Authorize]`, so even an opted-in host is authenticated-only — **never anonymous
  Add/Update/Delete**. Hardening-1's `GenericEntitySyncGuard` stays as the deeper backstop for the 17 financial
  entities.
- **`AizenGenericApi` (module reads):** **not registered by default** (opt-in `GenericEntityApi:Enabled`, default
  off) + `[AllowAnonymous]` → `[Authorize]`.
- **Internal service-token (`BffAssertion`):** **enforced fail-closed outside Development** — a non-empty
  `SharedSecret` is now required in non-dev or the host refuses to start (clear config error). Development with an
  empty secret still means "feature disabled" (unchanged). Existing empty-allowlist impersonation guard retained.
  Internal endpoints already `[Authorize]` + honor the assertion (confirmed).

Rationale: opt-in-off makes the routes **absent** by default (smallest attack surface); the `[Authorize]` is the
second lock for any future opt-in; the assertion enforcement makes the internal identity path a **configured,
required** control in prod rather than an inert default.

---

## Implementation (surgical, fail-closed)

1. **`Core/Starter/.../Generic/AizenGenericBffApi.cs`** — `[AllowAnonymous]` → `[Authorize]`.
2. **`Core/Starter/.../AizenBffServiceConfiguration.cs`** — register `BffRestControllerFeatureProvider` **only when**
   `configuration.GetValue<bool>("GenericBffCrud:Enabled")` (default false).
3. **`Core/Api/.../GenericApi/AizenGenericApi.cs`** — `[AllowAnonymous]` → `[Authorize]`.
4. **`Core/Api/.../Extentions/BuilderExtensions.cs`** (`AddAizenApi`) — register `GenericRestControllerFeatureProvider`
   **only when** `configuration.GetValue<bool>("GenericEntityApi:Enabled")` (default false).
5. **`Core/InfoAccessor/.../Extensions/BuilderExtensions.cs`** (`AddAizenInfoAccessor`) — added a second
   `.Validate(...)` on `AizenBffAssertionOptions`: **outside Development, `SharedSecret` must be non-empty** (clear
   error naming `AIZEN_BFF_ASSERTION_SECRET`), alongside the existing allowlist guard, still `.ValidateOnStart()`.
   Environment read from configuration (`ASPNETCORE_ENVIRONMENT`/`DOTNET_ENVIRONMENT`, env-var fallback, default
   Production) — no new argument, so every existing caller is unaffected and it's hermetically testable.
6. **Config/docs** — `docker-compose.yaml` comment updated to state the fail-closed semantics (no value change; the
   `${AIZEN_BFF_ASSERTION_SECRET:-}` wiring already existed). New operator note
   `docs/V1.0.1/Architecture/DEPLOY_SECRETS_BFF_ASSERTION.md` documents the required per-host secret + allowlist +
   the two opt-in flags.

Unchanged: internal endpoints, named BFF routes, publishing, two-phase commit, the assertion plumbing itself, and
every authenticated flow. No secret value added to the repo.

---

## Tests

New project `Core/InfoAccessor/tests/Aizen.Core.Hardening.UnitTests` (xunit + FluentAssertions; added to
`Aizen.sln`). **15 tests, all green.**

1. **No anonymous generic surface remains** — `AizenGenericBffApi<>` and `AizenGenericApi<>` each carry
   `[Authorize]` and **no** `[AllowAnonymous]` (class level); **specifically** every BFF verb
   (`AddEntity`/`UpdateEntity`/`DeleteEntity`/`GetEntity`/`SearchEntityForList`/`SearchEntityForPaged`) carries no
   method-level `[AllowAnonymous]` override → proves no anonymous write/delete/read.
2. **Non-dev empty secret fails closed** — `NonDev_empty_secret_fails_closed` + parameterized
   `Any_non_development_environment_requires_the_secret` (Production/Staging/QA): resolving
   `IOptions<AizenBffAssertionOptions>.Value` throws `OptionsValidationException` ("*required outside Development*").
   Because the same validator runs under `.ValidateOnStart()`, a non-dev host with an empty secret **will not boot**.
3. **Secret set works** — `NonDev_with_secret_and_allowlist_boots`: Production + secret + allowlist → no throw,
   value bound.
4. **Dev unchanged** — `Development_empty_secret_still_boots_feature_disabled`: Development + empty secret → no throw.
5. **Impersonation guard intact** — `Secret_set_but_empty_allowlist_still_fails_impersonation_guard`: secret set +
   empty allowlist → throws ("*AllowedClientIds is empty*").

Build:
- **Full solution `dotnet build Aizen.sln`: 0 Errors** (all hosts compile with the auth/flag/validation changes).
- The generic controllers still compile as authenticated types; `GenericEntitySyncGuard` (hardening-1) untouched.

---

## Verify (per the doc)

1. **Anonymous generic CRUD surface is gone.** By default the generic controllers are **not registered** (opt-in
   flags off) → the routes don't exist (404). If ever opted in, `[Authorize]` refuses an unauthenticated
   Add/Update/Delete/read (401) with no persistence — proven at the type level by test group 1 (no
   `[AllowAnonymous]` on class or any verb; `[Authorize]` present). No anonymous write/delete remains.
2. **Authenticated + internal flows keep working.** Only `[AllowAnonymous]` was removed from the two generic
   controllers; named BFF routes, `[Authorize]` internal endpoints (Payment/ReferenceData/dispute-state), and the
   assertion plumbing are untouched → the full solution builds and the internal identity path is unchanged for a
   valid service-token + assertion.
3. **Non-dev startup requires the secret (fail-closed); dev unchanged.** Proven by tests 2–4; compose defaults to
   Development so local runs are unaffected.

**Remaining live check (needs infra — RabbitMQ/Postgres/Redis/Keycloak):** boot each host image in a non-dev
profile to observe the startup refusal with an empty secret and success with it set, and exercise a real
BFF→module assertion round-trip. Both are exercised at the validation/DI layer by the passing unit tests (the
`ValidateOnStart` validator is the same code path the host runs at boot).

## Required per-host secret (deploy)

`BffAssertion:SharedSecret` (`BffAssertion__SharedSecret`, from `AIZEN_BFF_ASSERTION_SECRET`) + non-empty
`BffAssertion__AllowedClientIds__*` are **required on every module + BFF host outside Development**. Values live in
env/k8s secrets, never the repo. Full operator detail: `DEPLOY_SECRETS_BFF_ASSERTION.md`. This completes the
go-live hardening. **Do NOT commit.**
