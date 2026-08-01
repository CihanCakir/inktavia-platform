# ADMIN OTP HANDOFF — Kickoff 2d (Keycloak): add `admin-panel-bff` audience to admin-panel tokens

> **Scope: Keycloak / infra only** (`infrastructure/keycloak/init.sh` + the running realm). Do NOT touch admin-web,
> bff-adminpanel code, Identity, provider-web, or CargoDry. This is the LAST blocker: the FE handoff now logs the admin
> in (dashboard renders as "Admin User"), but the first `bff-adminpanel` call returns **401**, which triggers a
> refresh→logout→/login bounce.

## Root cause (confirmed by decoding the live access token)
The `admin-panel` public client mints the FE access token. bff-adminpanel validates it with (compose env, correct —
do NOT change): `AdminPanelKeycloak:Authority=http://localhost:8080/realms/inktavia-realm` (→ `ValidIssuer`),
`ValidateAudience=true`, `ValidAudience=admin-panel-bff`. The live token decodes to:
```
iss = http://localhost:8080/realms/inktavia-realm        ✅ matches ValidIssuer
azp = admin-panel                                        (the FE client)
realm_access.roles ⊇ { Admin }                            ✅
aud = ["notification-api","messaging-api","cargodry-api"] ❌ does NOT contain "admin-panel-bff"
```
→ .NET JWT bearer audience validation fails (token.aud has no `admin-panel-bff`) → **401 at authentication, upstream of
the `AdminPanelAccess` policy** (which is why it's 401, not 403). Issuer and signing key are fine.

The realm export (`infrastructure/keycloak/inktavia-realm-realm.json`) already declares the correct mapper on the
`admin-panel` client — `audience-admin-panel-bff` (`oidc-audience-mapper`, `included.client.audience=admin-panel-bff`)
— but the **running** realm doesn't have it (the realm was imported before the mapper existed; import is one-time). The
running `admin-panel` client instead carries stale audience mappers (notification/messaging/cargodry). So the fix is to
make the running client emit `aud: admin-panel-bff`, and to make `init.sh` apply it durably (mirrors the provider
pattern: `provider-portal` → audience `provider-portal-bff`).

## Change (idempotent, mirror the existing init.sh style)
In `infrastructure/keycloak/init.sh`, after the realm/SSL setup and alongside the admin-OTP-flow block, add an
idempotent step that ensures the `admin-panel` client has an **oidc-audience-mapper** adding `admin-panel-bff` to the
access token:
1. Resolve the `admin-panel` client id: `kcadm get clients -r inktavia-realm -q clientId=admin-panel --fields id`.
2. If a protocol mapper named `audience-admin-panel-bff` does not already exist on that client, create it:
   ```
   kcadm create clients/<id>/protocol-mappers/models -r inktavia-realm \
     -s name=audience-admin-panel-bff \
     -s protocol=openid-connect \
     -s protocolMapper=oidc-audience-mapper \
     -s 'config."included.client.audience"=admin-panel-bff' \
     -s 'config."id.token.claim"=false' \
     -s 'config."access.token.claim"=true'
   ```
   Guard creation so re-runs don't duplicate (check existing mappers first) — same idempotency discipline as the
   provider-otp-login flow block.
3. (Optional, to match the export exactly) remove the stale `notification-api`/`messaging-api`/`cargodry-api` audience
   mappers from the `admin-panel` client if present — they are wrong for a panel client. Not required for the fix
   (adding `admin-panel-bff` is sufficient, since .NET passes audience validation when ANY `aud` entry matches
   `ValidAudience`), but cleaner. Leave it out if it adds risk.

Re-run the keycloak-init step (`docker compose up keycloak-init`, or restart the init container) so the running realm
picks it up. No realm re-import, no service rebuilds.

## Acceptance / verification
1. Decode a fresh `admin-panel` access token (log in from `http://localhost:3000/login` as `admin.user@inktavia.com`
   → OTP from `docker compose logs identity-api` → handoff) and confirm `aud` now **contains `admin-panel-bff`** (the
   other entries may remain).
2. `GET http://localhost:17001/api/v1/admin-panel/dashboard/overview` with that Bearer token → **200** (was 401);
   the notifications/unread-count call → 200; the app **stays** on `/app/dashboard` (no refresh→/login bounce).
3. `[Authorize(Policy="AdminPanelAccess")]` admits the `Admin` realm role (already present in the token). Non-admin
   user → 403 (authorization), not 401 — confirming the auth layer now passes.
4. Provider realm/clients untouched; `admin-panel-bff` service-token mappers (the 10 API audiences) untouched.

## Report
`docs/V1.0.1/Identity/REPORT_ADMIN_OTP_HANDOFF_2D.md`: the init.sh diff, the before/after token `aud`, and the BFF
200 transcript. This closes the admin OTP login end-to-end (FE handoff + BFF authorization).
