# REPORT — BE_M2b_KEYCLOAK_PARTICIPANT_FLOW

Keycloak `init.sh` wiring so a `login_ticket` minted by the M2a participant OTP-login vertical (for the
`inktavia-mobile` client) is validated + consumed by the shared OTP-login SPI, plus a durable
`marine-mobile-bff` confidential client and the `inktavia-mobile` → `marine-mobile-bff` audience mapper.
Keycloak/infra only; mirrors the existing "Admin OTP Login browser" block.

**Verdict: ✅ M2b VERIFIED** — init runs clean & idempotent, all four objects exist with correct config,
Provider/Admin flows are untouched, and the end-to-end SPI validated+consumed a live participant ticket
(final user-resolution fails only on the M2a dev-placeholder subject → that is M2d).

---

## 1. `init.sh` additions (all additive, idempotent, guarded where relevant)

One new section after the Admin block ("── Participant (mobile) OTP Login flow + durable
marine-mobile-bff client ──"), mirroring the Admin patterns (kcadm + the existing `_first_id`/`_id_where`
sed/grep helpers — the KC25 image has no python3/jq/curl):

- **A) Durable `marine-mobile-bff` confidential client (create-if-absent):** if missing on the running
  realm, create mirroring `admin-panel-bff` — `publicClient:false`, `serviceAccountsEnabled:true`,
  `standardFlowEnabled:false`, `directAccessGrantsEnabled:false`, secret from
  `KEYCLOAK_MARINE_MOBILE_BFF_CLIENT_SECRET` (fallback `local-dev-only-change-me`), then loop-add
  `oidc-audience-mapper`s for the full mobile module set (identity, profile, vessel, file-storage,
  service-request, reference-data, cargodry, messaging, notification). Skips if the client already exists.
- **B) `audience-marine-mobile-bff` mapper on `inktavia-mobile` (add-if-missing):** `oidc-audience-mapper`
  with `included.client.audience=marine-mobile-bff`, `access.token.claim=true`, guarded by a
  grep-count so a re-run doesn't duplicate it.
- **C) "Participant OTP Login browser" flow + bind (guarded by `OTP_LOGIN_TICKET_SECRET` +
  `OTP_LOGIN_CONSUME_SECRET`):** copy of `browser`; execution `provider-otp-login-ticket` set
  ALTERNATIVE and raised to top; authenticator config alias `participant-otp-login-config` with
  `identityBaseUrl=http://identity-api:8080`,
  `consumePath=/api/v1/identity/auth/participant-otp-login/consume-ticket`,
  `ticketSecret`/`consumeSecret` from env, `allowedClientId=inktavia-mobile`, `maxSkewSeconds=30`; then
  bind `inktavia-mobile`'s `authenticationFlowBindingOverrides.browser` to the flow via the KC25
  partial-JSON `-b` trick — applied ALWAYS (idempotent), touching only the binding (its existing config
  and 9 audience mappers are left intact). Secrets are never echoed.

The SPI jar mount and the existing Provider/Admin flows/blocks were left byte-for-byte.

> **Repo note (important):** `infrastructure/keycloak/init.sh` is **git-ignored** by the repo's
> `.gitignore` line 10 (`*.sh`) — it has never been git-tracked (the Provider/Admin blocks aren't either).
> The `keycloak-init` container consumes it via a bind-mount (`./infrastructure/keycloak/init.sh:/init/init.sh:ro`),
> so the edit is live and was verified end-to-end below, but it does **not** appear in `git status`.

---

## 2. kcadm verification (secrets masked)

**marine-mobile-bff exists (confidential, service accounts):**
```json
{ "id":"6952ee91-…", "clientId":"marine-mobile-bff",
  "serviceAccountsEnabled":true, "publicClient":false }
```

**inktavia-mobile — flow binding + audience mapper (from the full client JSON):**
```
"authenticationFlowBindingOverrides" : { "browser" : "0fe5484c-ea5a-40b0-9a0f-4891588e8f57" }   # = Participant OTP Login browser
protocol-mapper "audience-marine-mobile-bff": oidc-audience-mapper,
                config.included.client.audience = marine-mobile-bff, access.token.claim = true
```
(`inktavia-mobile` stays `publicClient:true`; only the binding + mapper were added.)

**Participant flow execution + authenticator config:**
```
execution: providerId=provider-otp-login-ticket, requirement=ALTERNATIVE, index=0 (top)
config participant-otp-login-config:
  allowedClientId = inktavia-mobile
  consumePath     = /api/v1/identity/auth/participant-otp-login/consume-ticket
  identityBaseUrl = http://identity-api:8080
  maxSkewSeconds  = 30
  ticketSecret    = ***MASKED***
  consumeSecret   = ***MASKED***
```

**flow id → alias cross-check:**
```
30d55566… = Provider OTP Login browser     (provider-portal binding)
962a4465… = Admin OTP Login browser        (admin-panel binding)
0fe5484c… = Participant OTP Login browser  (inktavia-mobile binding)
```

---

## 3. Idempotency (re-run)

`keycloak-init` was recreated and run a **second** time → exit 0, no errors, and every object reported
already-present:
```
marine-mobile-bff client already exists, skipping creation.
inktavia-mobile: audience mapper 'audience-marine-mobile-bff' already present.
Participant OTP Login browser flow already exists, skipping creation.
inktavia-mobile bound to 'Participant OTP Login browser' flow (FLOW_ID=0fe5484c-…).
Done.
```
Post-run counts (all **1** → no duplication):
```
Participant flows                                = 1
marine-mobile-bff clients                        = 1
audience-marine-mobile-bff mappers on inktavia-mobile = 1
provider-otp-login-ticket execs in participant flow   = 1
```

---

## 4. Provider/Admin flows untouched

Bindings verified intact after the M2b run:
```
provider-portal → browser override 30d55566… (Provider OTP Login browser)
admin-panel     → browser override 962a4465… (Admin OTP Login browser)
```
The Provider/Admin `init.sh` blocks were not modified. (The Provider block still logs its known
pre-existing `WARN: binding skipped FLOW_ID='' PORTAL_ID=''` from its python3 id-extraction, which
this image lacks — a prior-existing condition, not introduced here; the persisted binding above is
unaffected.)

---

## 5. End-to-end ticket (M2a + M2b)

Minted a real participant `login_ticket` via M2a (`request`→`verify` on identity-api :7101, IdentityWrite
service token), then drove Keycloak authorize for `inktavia-mobile` **with PKCE** (the client enforces
`code_challenge_method`; without it Keycloak 302s `invalid_request` before the flow runs):

```
GET /realms/inktavia-realm/protocol/openid-connect/auth
    ?client_id=inktavia-mobile&response_type=code&scope=openid
    &redirect_uri=http://localhost:19006/auth/callback
    &code_challenge=<S256>&code_challenge_method=S256&login_ticket=<ticket>
→ HTTP 200 (flow executed)
```

Keycloak SPI log (this attempt, `jti=e515fb44…`):
```
DEBUG [otp-login] Ticket verified. sub=00000000-0000-0000-0000-participant1
                  clientId=inktavia-mobile jti=e515fb44… exp=… now=…
WARN  [otp-login] No Keycloak user for sub '00000000-0000-0000-0000-participant1' in realm 'inktavia-realm'.
```

Interpretation (all four M2b integration points proven):
- The **participant flow ran** on the `inktavia-mobile` authorize request → binding works.
- **HMAC verified** (SPI's `ticketSecret` == Identity's) and **`clientId=inktavia-mobile` matched
  `allowedClientId`** → per-flow config works.
- **Ticket consumed:** there is **no** `Consume failed`/`HTTP 410` WARN for this jti (contrast the older
  provider/admin 410 examples), and the ticket **jti is absent from Redis** (a Redis scan found no
  `otplogin:ticket:e515fb44…` key) → the SPI's call to the **participant consume path** returned 200 and
  single-use-deleted the jti.
- The flow then failed **only** at Keycloak user-resolution because the subject is the M2a
  **dev placeholder** (`00000000-0000-0000-0000-participant1`), not a real Keycloak subject. Replacing it
  with the real subject (the participant equivalent of the admin `admin-provision` linkage already in
  `init.sh`) is **M2d**, explicitly out of scope here.

So the M2a→M2b chain — mint → SPI validate → SPI consume — is fully verified; the last hop (real user)
is the M2d provisioning step.

---

## 6. Scoped `git status`

My only change this slice is `infrastructure/keycloak/init.sh`, which is **git-ignored** (`*.sh`) and
therefore does not surface in `git status`. Everything `git status` currently shows is **pre-existing,
unrelated parallel work that I did not touch** (per the task's instruction to ignore it):

```
 M Modules/Identity/.../InktaviaStoreIdentityDbContext.cs      # ProviderServiceCategory (not mine)
 M Modules/Identity/.../DependencyInjection.cs                 # ProviderServiceCategory (not mine)
 M Modules/Identity/.../Service/Onboarding/ProviderOnboardingDomainService.cs   # not mine
 M Modules/Notification/**                                     # notification work (not mine)
?? Modules/Identity/.../ProviderEligibility/, ProviderServiceCategory/          # not mine
?? Modules/Notification/.../Payment/PaymentAuthorizedConsumer.cs,
   Modules/Payment/.../PaymentAuthorizedMessage.cs             # not mine
?? docs/V1.0.1/{Identity,Mobile,Notification}/*.md            # incl. this report + the M2a report
```

No BFF, provider-web, admin-web, CargoDry, or Provider/Admin OTP-login files were changed. The durable
`marine-mobile-bff` client, the audience mapper, and the participant flow now live in the running
Keycloak realm DB (idempotently re-created by `init.sh` on every `keycloak-init` run).
