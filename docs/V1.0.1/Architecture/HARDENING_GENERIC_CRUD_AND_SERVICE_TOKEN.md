# HARDENING-2 (plan/diagnostic-first) — lock down the anonymous generic-CRUD surface + enforce internal service-token

> **Repo:** `addesso-project` — framework (`Core/Starter/src/Aizen.Core.Starter.Bff/Generic`, `Core/Api/src/Aizen.Core.Api/GenericApi`,
> `Core/InfoAccessor`) + per-host config. **Security-sensitive, framework-wide → diagnostic-first, decide, then a surgical,
> fail-closed change.** This is the second half of the go-live hardening (after the messagebus opt-out). **Do not commit**
> until the user says.

## The exposure (confirmed in source)
- **`AizenGenericBffApi<TEntity>` (BFF)** is annotated **`[AllowAnonymous]`** and exposes, per entity, **`GetEntity` +
  `AddEntity` (POST) + `UpdateEntity` (PUT) + `DeleteEntity` (DELETE)** + search. That's an **anonymous full-CRUD surface on
  every entity** hosted by a BFF. The messagebus opt-out (hardening-1) only guards the **17 financial entities** at the
  command-handler level — the **other ~145 entities remain anonymously creatable/updatable/deletable** via this endpoint if
  the network can reach it.
- **`AizenGenericApi<TEntity>` (module)** is also `[AllowAnonymous]` but **read-only** (Get/Search) — anonymous reads of
  every entity. Lower severity, still worth closing.
- **Internal service-token already exists:** `BffAssertion` — `AizenUserInfoMiddleware` accepts a trusted-BFF identity
  assertion **only** when the caller is a machine-to-machine **Keycloak service-token** AND presents a valid
  `X-Aizen-Bff-Assertion` == `BffAssertion:SharedSecret`. **But it's disabled when the secret is empty** (defaults to
  "behaves exactly as before"). So the mechanism is there; the hardening is **enforcement + config**, not new plumbing.

## Phase 0 — diagnostic (decides remove-vs-auth-vs-disable)
1. **Who calls the generic BFF CRUD routes?** Grep the FE repos + any client for the generic route shape
   (`AizenGenericBffApi` route template / `/generic/` / the entity-CRUD paths) and check server logs/usage. Determine
   whether **any real client** uses `AddEntity`/`UpdateEntity`/`DeleteEntity` (or the reads), or whether it's **unused
   scaffolding**. (Hardening-1 found no code publishes `AizenGenericMessage`, but these are a **separate HTTP surface** —
   verify independently.)
2. **Is the generic API registered on every host, or opt-in?** Find where `AizenGenericBffApi`/`AizenGenericApi` controllers
   get registered (controller feature provider / assembly scan) — is it automatic for all entities on all hosts?
3. **Is `BffAssertion:SharedSecret` configured** in each environment (appsettings/compose/k8s secrets), or empty (→ the
   assertion path is inert and internal endpoints rely on `[Authorize]` + network policy alone)?
4. **Are the internal module remote-call endpoints `[Authorize]`** (not anonymous), and do they depend on the assertion for
   identity? Sample the ones we've built (Payment `ResolveLineCommissions`, ReferenceData resolve, dispute payment-state,
   etc.).
Record the findings — they gate the decision.

## Decision (gated on Phase 0)
- **Generic BFF CRUD (`AizenGenericBffApi`):** headline. If **unused** → **disable it by default** (don't register the
  generic CRUD controllers; opt-in per host/entity if ever needed). If **something uses reads** → keep **reads behind
  `[Authorize]`** (drop `[AllowAnonymous]`) and **remove/disable the write/delete verbs** (Add/Update/Delete) unless a
  specific, authorized, audited need exists. **Never leave anonymous Add/Update/Delete on.** Prefer fail-closed:
  authenticated + authorized (AdminPanelAccess or a dedicated policy), or off.
- **Generic module API (`AizenGenericApi`) reads:** drop `[AllowAnonymous]`; require `[Authorize]` (or restrict to the
  internal service-token path) so entity reads aren't anonymous. If genuinely internal-only, require the service-token.
- **Internal service-token:** make `BffAssertion` **enforced, fail-closed in non-dev** — require a non-empty
  `BffAssertion:SharedSecret` (fail startup or reject asserted calls if missing outside Development), confirm every internal
  remote-call endpoint is `[Authorize]` + honors the assertion, and document the secret as a required env/secret per host.
  (Do **not** hand-hold a plaintext secret in the repo — it lives in env/k8s secrets, gitignored.)

## Implementation (once decided)
1. **`AizenGenericBffApi`:** remove `[AllowAnonymous]`; either don't register it (opt-in flag, default off) or gate it with
   `[Authorize(Policy = …)]` and strip/guard the write/delete actions. Keep the hardening-1 `GenericEntitySyncGuard` as the
   deeper backstop.
2. **`AizenGenericApi`:** remove `[AllowAnonymous]`; `[Authorize]` (or service-token-only).
3. **`BffAssertion` enforcement:** in `AizenUserInfoMiddleware`/startup, **require a non-empty secret outside Development**
   (fail-closed); reject a BFF-assertion call that lacks the M2M service-token or the secret. Surface a clear config error
   if the secret is missing in a non-dev environment.
4. **Config:** document `BffAssertion:SharedSecret` (+ the internal audiences) as required per host in the deploy notes;
   ensure compose/k8s wire it from secrets (values not in the repo).

## Don't-break / QA
- Surgical + fail-closed: the change is auth annotations + a registration/opt-in flag + assertion enforcement + config.
  **Legitimate authenticated flows (admin panel, provider/owner BFFs, internal remote calls with the service-token) keep
  working**; only the **anonymous** generic surface is closed. If Phase 0 finds a real anonymous consumer, migrate it to an
  authenticated path rather than leaving the hole.
- Tests: (1) an anonymous request to a generic BFF write/delete (or the whole generic surface) is **rejected** (401/404, no
  write) — prove for a sample entity; (2) an anonymous generic read is rejected (or service-token-only); (3) an authenticated
  admin request still works where intended; (4) a valid BFF-assertion internal call still resolves identity; (5) startup in
  a non-dev profile with an **empty** `BffAssertion:SharedSecret` **fails closed** (or rejects asserted calls), and with the
  secret set it works. Build + boot all hosts; no regression in the internal remote-call paths we've built.

## Verify
1. The anonymous generic CRUD surface is gone: an unauthenticated Add/Update/Delete/read against a generic entity route is
   refused (no persistence) — checked for a sample entity and specifically confirmed no anonymous write/delete remains.
2. Admin-panel + provider/owner BFF flows and the internal remote calls (Payment/ReferenceData/dispute state) still work
   with their tokens/assertion.
3. Non-dev startup requires the `BffAssertion` secret (fail-closed); dev behaviour is unchanged.

## Report
`docs/V1.0.1/Architecture/REPORT_HARDENING_GENERIC_CRUD_SERVICE_TOKEN.md`: the Phase-0 findings (who used the generic
surface, secret config per env), the remove-vs-auth-vs-disable decisions + rationale, the exact changes (generic BFF/module
API auth, assertion fail-closed, config), the tests proving the anonymous surface is closed with no regression, and the
required per-host secret documentation. This completes the go-live hardening. **Do NOT commit.**
