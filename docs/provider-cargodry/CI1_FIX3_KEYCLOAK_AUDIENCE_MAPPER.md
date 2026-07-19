# CI-1 FIX 3 — add `cargodry-api` to the provider-portal-bff service-token audience (Keycloak)

## Why (root cause, confirmed)
`GET /provider/cargodry/overview` → BFF 500 wrapping **401 from the cargodry-api module**:
```
header.errorCode = 911
"Response status code does not indicate success: 401 (Unauthorized)."
  at IProviderCargoDryRemoteCall.GetOverview()
```
Program.cs (`AddAizenInfoAccessor`) and compose (`BffAssertion__*`) are **already correct** — that is NOT the
blocker. The blocker is one layer earlier: the module's JWT bearer authentication.

- Every module validates `aud == KEYCLOAK_AUDIENCE` (`AddAizenKeycloakAuth`: `ValidateAudience = true`,
  `ValidAudience = "cargodry-api"`) and applies a `RequireAuthenticatedUser` fallback policy to all endpoints.
- The BFF calls modules with a **client_credentials service token** for `provider-portal-bff`
  (`ProviderKeycloakServiceTokenProvider`, plain grant — no audience param). The token's `aud` is decided
  entirely by the audience mappers on the `provider-portal-bff` Keycloak client.
- That client has per-module audience mappers added out-of-band (repo docs confirm `reference-data-api`,
  and the working `service-request-api`/`vessel-api`/… ) — but **NOT `cargodry-api`**. So the service token
  has no `cargodry-api` in `aud` → cargodry-api rejects it with 401. (`setup-provider-realm.sh` historically
  only added `aud-provider-portal-bff` to the public client; the service-token module audiences were added
  manually.)

## Fix — add the missing audience mapper on the `provider-portal-bff` client
Run against the **running** Keycloak (realm `inktavia-realm`). `setup-provider-realm.sh` has already been
updated to emit this for all callable modules on future runs; this command applies it now without a full re-run.

```bash
# from inside the keycloak container, or with kcadm.sh on PATH
export KEYCLOAK_URL=http://localhost:8080 REALM=inktavia-realm
kcadm.sh config credentials --server "$KEYCLOAK_URL" --realm master \
  --user "$KEYCLOAK_ADMIN" --password "$KEYCLOAK_ADMIN_PASSWORD"

BFF_ID=$(kcadm.sh get clients -r "$REALM" -q clientId=provider-portal-bff \
  --fields id --format csv --noquotes | tail -n1)

kcadm.sh create "clients/$BFF_ID/protocol-mappers/models" -r "$REALM" \
  -s name=aud-cargodry-api \
  -s protocol=openid-connect \
  -s protocolMapper=oidc-audience-mapper \
  -s 'config."included.client.audience"=cargodry-api' \
  -s 'config."access.token.claim"=true'
```

**Admin Console alternative:** Clients → `provider-portal-bff` → Client scopes →
`provider-portal-bff-dedicated` → Add mapper → **Audience** → Name `aud-cargodry-api`,
Included Client Audience = `cargodry-api`, Add to access token = ON.

## Then bust the cached BFF token
`ProviderKeycloakServiceTokenProvider` caches the service token (`expires_in - 60s`). The mapper only affects
**newly issued** tokens, so:
- **Restart `bff-marineprovider`** (drops the in-memory token cache), or wait for the current token to expire.
- No cargodry-api / compose rebuild is needed — nothing changed in those images this round.

## Acceptance
- `GET /provider/cargodry/overview` → **200** (overview DTO), no 401/500.
- `GET /provider/cargodry/alerts?take=5` → 200.
- Decode the BFF service token (optional): `aud` now includes `cargodry-api`.
- Existing provider ServiceRequest/Vessel calls still work (only an audience was added).

## Follow-up (unchanged, still open)
Overview/alerts handlers accept `ProviderProfileId` but don't filter by it yet — they return GLOBAL totals.
Thread the provider filter before CI-2 so a provider cannot see platform-wide numbers.
