# Provider-portal provisioning + KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET keystone + wipe self-heal

Problem: provider-portal login can't complete. The provider user self-heals now (dev-seed companion), but the
`provider-portal` / `provider-portal-bff` Keycloak clients + provider OTP flow are NOT created by init.sh — they
come from `infrastructure/keycloak/provider-realm/setup-provider-realm.sh`, which requires
`KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET`. That var has been unset/blank all session (the "ignore this WARN" one).

**Keystone finding — this secret is bigger than provider portal.** `KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET` is the
`provider-portal-bff` confidential-client secret, and that client is used as the **Keycloak admin client by
multiple services**, not just the provider BFF:
- `marine-provider-bff`: `MarineProviderKeycloak__AdminClientSecret` (compose ~220, ~368),
- **`identity-api`**: `IdentityKeycloak__AdminClientId: provider-portal-bff` + `IdentityKeycloak__AdminClientSecret:
  ${KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET}` (compose ~466-467) — **identity-api authenticates to Keycloak for its
  admin operations (user provisioning, subject linkage) with this exact client/secret.**
- BFF-assertion allow-lists reference `provider-portal-bff` widely.

So while it's blank, identity-api's Keycloak admin path is silently degraded — very likely the source of the
manual admin-subject reconciliation + provisioning pain we hit all session. Setting it consistently should both
unblock provider login AND stabilize identity-api's Keycloak admin operations.

## Do
1. **Set the secret once, consistently (dev, gitignored .env, uncommitted):** `KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET=
   <dev-secret>`. Confirm `docker compose config` resolves the SAME value everywhere it's consumed:
   provider-portal-bff client creation (setup script), marine-provider-bff, identity-api
   (`IdentityKeycloak__AdminClientSecret`), and any provider assertion. It must match the secret the Keycloak
   client is created with — a mismatch is as bad as blank.
2. **Provision the provider realm objects:** run `infrastructure/keycloak/provider-realm/setup-provider-realm.sh`
   against the running Keycloak (realm inktavia-realm) with `KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET` + KC admin
   creds set. It creates `provider-portal` (public SPA), `provider-portal-bff` (confidential + service account
   with the secret), realm roles, provider_profile_id + audience mappers, verify-email/forgot-password, and the
   provider-portal-bff service-account roles (realm-management + identity-api). It uses `kc` (kcadm) so it can run
   in the KC 25 image.
3. **Bind the provider OTP flow:** ensure the "Provider OTP Login browser" flow is bound to `provider-portal`
   (init.sh's `_kc_bind_browser_flow "provider-portal"` already does this, but it needs the client to EXIST first
   → order it AFTER setup-provider-realm.sh). Confirm the provider authenticator config carries the OTP ticket
   secret (init.sh self-heals this when the OTP secrets are set).
4. **Recreate the consumers** so they pick up the now-non-empty secret: `identity-api`, `marine-provider-bff`
   (+ provider-portal-bff-dependent services). Verify identity-api can now obtain a Keycloak admin token with
   `provider-portal-bff` (no blank-secret auth failures in its logs).
5. **Fold into wipe self-heal (the real ask):** make the provider realm setup run automatically on bring-up so a
   `keycloak-db` wipe restores provider parity with admin/participant — either invoke `setup-provider-realm.sh`
   from `init.sh` (or from the `dev-seed-kc-users.sh` companion) in the dev gate, idempotently, ordered before the
   provider flow binding. Keep it dev-gated; the secret comes from env, never hardcoded.

## Verify (clean-wipe provider parity)
- `docker volume rm <stack>_kc_pg_data` + realm re-import + `keycloak-init` + the companion — with NO manual
  kcadm/REST commands afterward — must leave provider fully loginnable:
  - provider clients exist (`provider-portal` public, `provider-portal-bff` confidential w/ the secret + SA roles),
  - provider OTP flow bound to `provider-portal`,
  - Provider OTP login E2E: request OTP → DEV-ONLY code → verify → login-ticket → provider-portal handoff →
    /auth/callback?code → RS256 token (aud provider-portal-bff, provider roles) → a protected provider BFF call 200.
- Confirm identity-api Keycloak admin ops work with the real secret (no blank-secret errors; admin/participant
  provisioning stays green — ideally the manual 1104 reconciliation is no longer needed once the admin client can
  actually authenticate).
- Report: the secret wiring (every consumer resolves the same value), the setup-script run output (clients/roles/
  mappers created), the self-heal integration point, and the clean-wipe provider login proof. Dev-only.
  Committed source changes (if init.sh integration touches tracked files) stay staged/uncommitted for the owner.
