# FIX (diagnostic-first) — SR→Payment S2S 401 on owner-initiated economics

> **Repo:** `addesso-project` (ServiceRequest + Payment; possibly compose/Keycloak config). Surfaced by the WC1 smoke:
> owner **accept** 500s because `SR → Payment CalculateServiceRequestEconomicsAsync` returns **401**. This blocks the
> `OFFER_ACCEPTED / JOB_STARTED / JOB_COMPLETED` chain **and the real owner accept flow (MO3)** on the BffAssertion
> path. **Pre-existing, not WC1.** **Diagnose first, then apply the minimal correct fix.** **Do not commit.**

## Root-cause hypothesis (from code — confirm before fixing)
`AcceptServiceRequestOfferCommandHandler` forwards the **caller's** token to Payment:
```csharp
var rawToken = _info.UserInfoAccessor.UserInfo.AccessToken; // forwarded to Payment module
var economics = await _paymentRemoteCall.CalculateServiceRequestEconomicsAsync(
    economicsRequest, $"Bearer {rawToken}", ct);
```
`PaymentInternalController` (`/api/v1/payment/internal/*`) is `[Authorize]` (any valid JWT). The forward works when a
**real user Keycloak JWT** flows through (admin/provider portal with a Keycloak session). **But the owner mobile accept
arrives on the BffAssertion path** (owner mobile → mobile-bff → SR): the owner's real Keycloak token **never reaches
SR** — only the assertion headers (`X-Aizen-Provider-Profile-Id`) + the mobile-bff **service token**. So
`UserInfo.AccessToken` is likely **empty or a non-payment-audience token** → `Bearer {empty}` → **401**.

This is almost certainly the **first live exercise of owner accept → P8 economics** (MO3 was build/route-verified, the
live capture was iyzico-gated, so the S2S economics call was never driven on the owner path before). **Blast radius:**
every owner-initiated SR→Payment forward that passes `UserInfo.AccessToken` — accept, change-order approve, dispute
resolve, release payment, owner payment-status — all share this pattern and will 401 on the assertion path.

## The real root cause — this call is built the WRONG way vs. our canonical S2S pattern (confirmed in code)
The manual token-forward is **inconsistent with how every other connection is established.** The canonical pattern is
an **ambient outgoing `DelegatingHandler`** (e.g. `MarineMobileBffAuthDelegatingHandler`) that, at the HTTP layer,
attaches a **Keycloak service-account token (client_credentials)** in `Authorization` **and** the BffAssertion headers
(`X-Aizen-Bff-Assertion` + `X-Aizen-User-Id` + `X-Aizen-Provider-Profile-Id`) — and **"never forwards a user token,
never fabricates a JWT"** (its own doc). The caller code never threads `Authorization`.

Evidence this call is the outlier:
- Only `IPaymentModuleRemoteCall`, `IPaymentFileStorageRemoteCall`, `IFileStorageRemoteCall`,
  `IIdentityFileStorageRemoteCall` declare a manual `[AizenRemoteCallHeader("Authorization")]` param;
  `IServiceRequestReferenceDataRemoteCall` (and the ambient-handler BFF calls) do **not**.
- The **SR module host registers no ambient outbound service-token handler** for its module→module calls — so these
  calls were left to manually forward the caller's bearer.

**Why it was written this way:** the SR→Payment internal call (P8 economics + escrow) predates / bypassed the ambient
service-token handler pattern — an early ad-hoc "forward the caller's bearer" that happened to work whenever a **real
user Keycloak JWT** flowed through (admin/provider portal with a direct session), and was never exercised on the
**assertion** path (owner mobile; any BFF-asserted caller). The owner accept is the first time it ran there → 401.
**So the correct fix is not "forward a better token" — it is to establish this connection the way every other
connection is established** (ambient service token), and drop the manual forwarding.

## Step 1 — DIAGNOSE (capture evidence; change nothing yet)
1. **Reproduce** the owner accept (mobile/assertion path) → capture the 401. Log/inspect the exact
   `UserInfo.AccessToken` value the SR handler forwards on this path: **empty? the mobile-bff service token? a token
   with the wrong `aud`?**
2. **payment-api rejection reason:** read payment-api auth logs for the 401 — is it *no token*, *invalid signature*,
   *wrong audience/issuer*, or *expired*? (payment-api validates against its Keycloak authority; note its expected
   audience.)
3. **Contrast the working path:** confirm the same call **succeeds** from the admin/provider path (real user JWT) — to
   prove the difference is the token source, not payment-api config.
4. **Rule out env drift:** the stack was rebuilt today — confirm it is **not** merely a cached service token / a
   post-rebuild config gap (Keycloak client secret, audience mapper) by checking whether a restart / correct config
   changes the outcome. Record which it is.

## Step 2 — FIX (choose per the diagnosis; minimal + correct)
- **If the assertion path has no forwardable user token (most likely):** the SR→Payment **internal** calls are
  service-to-service — they should authenticate with a **dedicated service token** (Keycloak **client-credentials**),
  not a forwarded caller token. The compose already provisions `KeycloakServiceToken__*` (Authority/TokenEndpoint/
  ClientSecret). Introduce/verify a service-token provider in SR and use it for the `IPaymentModuleRemoteCall`
  `Authorization` on all internal calls (accept economics, escrow, release, change-order, dispute-state,
  payment-status). This makes the calls independent of the caller's token source (works for owner *and* provider *and*
  admin uniformly). Ensure payment-api accepts the service token's audience (add an audience mapper / accept the
  service client if needed — the recurring [[provider_bff_module_audience_mapper]] pattern; **restart** payment-api to
  bust any cached token after a config change).
- **If it is an audience mismatch on a token that IS present:** align the forwarded token's `aud` with payment-api's
  expected audience (mapper), rather than switching mechanisms.
- **If it is purely env drift (cached token / missing secret after rebuild):** fix the config/secret + restart; no code
  change.

Prefer the **service-token** approach for the internal S2S calls if the diagnosis confirms the assertion path lacks a
forwardable token — it is the durable fix and removes the "owner path 401" class entirely. Keep the change minimal and
do **not** loosen `PaymentInternalController` auth (no `[AllowAnonymous]`, no BffAssertion-only bypass).

## Step 3 — VERIFY (blast radius)
- Owner **accept** → `CalculateServiceRequestEconomics` **200**, escrow created, offer Accepted (dev/manual gateway
  path); then the smoke's `OFFER_ACCEPTED / JOB_STARTED / JOB_COMPLETED` System messages can be driven.
- Re-check the **other owner-initiated forwards**: change-order approve (P8 incremental), dispute resolve, release
  payment, owner payment-status — each reaches Payment without 401 on the assertion path.
- **No regression** on the admin/provider path (real user JWT still works).
- Confirm idempotency/economics unchanged (this is an **auth** fix, not an economics change).

## Don't-break / QA
- Auth-only change; **no economics/ledger/escrow logic touched**. `PaymentInternalController` stays `[Authorize]`.
  Secrets stay in env/compose (never committed/printed). If a service-token client is introduced, its secret is
  env-provided.
- Tests: (1) SR + Payment build 0 errors; (2) the internal call carries a valid token accepted by payment-api
  independent of the caller path (unit/integration around the token provider); (3) owner accept end-to-end 200 (dev
  gateway); (4) admin/provider path unregressed; (5) the 5 forward sites all authenticate. Live smoke = owner accept →
  economics 200 → then re-run the WC1 3-code smoke.

## Report
`docs/V1.0.1/Architecture/REPORT_FIX_PAYMENT_S2S_401.md`: the diagnosed root (what token flowed, payment-api's 401
reason, config-vs-code), the chosen fix (service token vs audience align vs env), the 5 forward sites verified, the
owner-accept-200 proof, and confirmation the admin/provider path is unregressed. On green → **re-run the WC1 smoke for
`OFFER_ACCEPTED / JOB_STARTED / JOB_COMPLETED`** → WC1 fully closed → **WC2**.
