#!/usr/bin/env bash
# ---------------------------------------------------------------------------
# Inktavia — Provider Web/PWA Keycloak setup (idempotent-ish) for realm inktavia-realm.
# Creates: provider-portal (public SPA), provider-portal-bff (confidential + service account),
# realm roles, provider_profile_id + audience mappers, Google IdP, verify-email/forgot-password,
# and provider-portal-bff service-account roles (realm-management + identity-api).
#
# Requires: Keycloak `kcadm.sh` on PATH (or run inside the keycloak container).
# NEVER hardcode secrets — pass them via environment variables.
#
# Usage:
#   export KEYCLOAK_URL=http://localhost:8080
#   export KEYCLOAK_ADMIN=admin KEYCLOAK_ADMIN_PASSWORD=admin
#   export KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET=...          # provider-portal-bff secret
#   export GOOGLE_OAUTH_CLIENT_ID=... GOOGLE_OAUTH_CLIENT_SECRET=...
#   export PROVIDER_WEB_BASE=http://localhost:3002          # SPA origin
#   ./setup-provider-realm.sh
# ---------------------------------------------------------------------------
set -euo pipefail

REALM="${REALM:-inktavia-realm}"
KEYCLOAK_URL="${KEYCLOAK_URL:-http://localhost:8080}"
KC_ADMIN="${KEYCLOAK_ADMIN:-admin}"
KC_ADMIN_PW="${KEYCLOAK_ADMIN_PASSWORD:-admin}"
PROVIDER_WEB_BASE="${PROVIDER_WEB_BASE:-http://localhost:3002}"
BFF_SECRET="${KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET:?set KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET}"
GOOGLE_ID="${GOOGLE_OAUTH_CLIENT_ID:-REPLACE_ME}"
GOOGLE_SECRET="${GOOGLE_OAUTH_CLIENT_SECRET:-REPLACE_ME}"

kc() { kcadm.sh "$@"; }

echo "==> Logging in to $KEYCLOAK_URL"
kc config credentials --server "$KEYCLOAK_URL" --realm master --user "$KC_ADMIN" --password "$KC_ADMIN_PW"

# --- Realm-level: login settings (verify email + forgot password) -----------
echo "==> Enabling Verify Email + Forgot Password on $REALM"
kc update "realms/$REALM" -s verifyEmail=true -s resetPasswordAllowed=true -s registrationEmailAsUsername=false

# --- Allow unmanaged user attributes (REQUIRED for provider_profile_id) -----
# Keycloak 24+ ships the declarative User Profile with unmanaged attributes DISABLED, so a
# provider_profile_id attribute set via the Admin API is silently dropped. ADMIN_EDIT lets the
# service account write it. Alternative: declare provider_profile_id in Realm → User profile.
echo "==> Allowing unmanaged user attributes (unmanagedAttributePolicy=ADMIN_EDIT)"
kc update users/profile -r "$REALM" -s 'unmanagedAttributePolicy=ADMIN_EDIT' 2>/dev/null \
  || echo "    (could not set via kcadm — declare provider_profile_id in Realm settings → User profile, or enable unmanaged attributes)"

# --- Realm roles ------------------------------------------------------------
for ROLE in provider_pending provider_user provider_restricted; do
  if ! kc get "roles/$ROLE" -r "$REALM" >/dev/null 2>&1; then
    echo "==> Creating realm role $ROLE"
    kc create roles -r "$REALM" -s name="$ROLE"
  else
    echo "==> Realm role $ROLE already exists"
  fi
done

# --- provider-portal (public SPA) ------------------------------------------
echo "==> Upserting client provider-portal"
PP_ID=$(kc get clients -r "$REALM" -q clientId=provider-portal --fields id --format csv --noquotes 2>/dev/null | tail -n +1 | head -n1 || true)
if [ -z "${PP_ID:-}" ]; then
  PP_ID=$(kc create clients -r "$REALM" -i \
    -s clientId=provider-portal \
    -s publicClient=true \
    -s standardFlowEnabled=true \
    -s directAccessGrantsEnabled=false \
    -s 'attributes."pkce.code.challenge.method"=S256' \
    -s "redirectUris=[\"$PROVIDER_WEB_BASE/*\"]" \
    -s "webOrigins=[\"$PROVIDER_WEB_BASE\"]")
fi
echo "    provider-portal id=$PP_ID"

# provider_profile_id user-attribute mapper on provider-portal
echo "==> Adding provider_profile_id mapper"
kc create "clients/$PP_ID/protocol-mappers/models" -r "$REALM" \
  -s name=provider_profile_id \
  -s protocol=openid-connect \
  -s protocolMapper=oidc-usermodel-attribute-mapper \
  -s 'config."user.attribute"=provider_profile_id' \
  -s 'config."claim.name"=provider_profile_id' \
  -s 'config."jsonType.label"=String' \
  -s 'config."id.token.claim"=true' \
  -s 'config."access.token.claim"=true' \
  -s 'config."userinfo.token.claim"=true' 2>/dev/null || echo "    (mapper may already exist)"

# audience mapper so provider-portal tokens include provider-portal-bff
echo "==> Adding audience mapper (provider-portal-bff)"
kc create "clients/$PP_ID/protocol-mappers/models" -r "$REALM" \
  -s name=aud-provider-portal-bff \
  -s protocol=openid-connect \
  -s protocolMapper=oidc-audience-mapper \
  -s 'config."included.client.audience"=provider-portal-bff' \
  -s 'config."access.token.claim"=true' 2>/dev/null || echo "    (audience mapper may already exist)"

# --- provider-portal-bff (confidential + service account) -------------------
echo "==> Upserting client provider-portal-bff"
BFF_ID=$(kc get clients -r "$REALM" -q clientId=provider-portal-bff --fields id --format csv --noquotes 2>/dev/null | tail -n +1 | head -n1 || true)
if [ -z "${BFF_ID:-}" ]; then
  BFF_ID=$(kc create clients -r "$REALM" -i \
    -s clientId=provider-portal-bff \
    -s publicClient=false \
    -s standardFlowEnabled=false \
    -s serviceAccountsEnabled=true \
    -s secret="$BFF_SECRET")
else
  kc update "clients/$BFF_ID" -r "$REALM" -s secret="$BFF_SECRET"
fi
echo "    provider-portal-bff id=$BFF_ID"

# --- provider-portal-bff service-token audience mappers ---------------------
# The BFF calls downstream modules with its client_credentials (service account) token.
# Each module validates aud == KEYCLOAK_AUDIENCE (e.g. cargodry-api), so the BFF token MUST
# carry every callable module id in its `aud`. One oidc-audience-mapper per module.
for AUD in identity-api reference-data-api vessel-api file-storage-api \
           service-request-api messaging-api notification-api cargodry-api payment-api; do
  echo "==> Adding service-token audience mapper aud-$AUD on provider-portal-bff"
  kc create "clients/$BFF_ID/protocol-mappers/models" -r "$REALM" \
    -s name="aud-$AUD" \
    -s protocol=openid-connect \
    -s protocolMapper=oidc-audience-mapper \
    -s "config.\"included.client.audience\"=$AUD" \
    -s 'config."access.token.claim"=true' 2>/dev/null \
    || echo "    (audience mapper aud-$AUD may already exist)"
done

# --- provider-portal-bff service-account roles ------------------------------
SA_USER=$(kc get "clients/$BFF_ID/service-account-user" -r "$REALM" --fields id --format csv --noquotes | tail -n1)
echo "==> Service account user id=$SA_USER"

# realm-management client roles
RM_ID=$(kc get clients -r "$REALM" -q clientId=realm-management --fields id --format csv --noquotes | tail -n1)
for RMROLE in view-users query-users manage-users; do
  echo "==> Granting realm-management:$RMROLE to service account"
  kc add-roles -r "$REALM" --uusername "service-account-provider-portal-bff" \
    --cclientid realm-management --rolename "$RMROLE" 2>/dev/null || echo "    (already granted or adjust manually)"
done

# identity-api client roles (identity_read / identity_write)
for IDROLE in identity_read identity_write; do
  echo "==> Granting identity-api:$IDROLE to service account"
  kc add-roles -r "$REALM" --uusername "service-account-provider-portal-bff" \
    --cclientid identity-api --rolename "$IDROLE" 2>/dev/null || echo "    (ensure identity-api client role $IDROLE exists, then re-run)"
done

# --- Google identity provider (social login) --------------------------------
echo "==> Upserting Google identity provider"
if ! kc get "identity-provider/instances/google" -r "$REALM" >/dev/null 2>&1; then
  kc create identity-provider/instances -r "$REALM" \
    -s alias=google \
    -s providerId=google \
    -s enabled=true \
    -s trustEmail=true \
    -s 'config."clientId"='"$GOOGLE_ID" \
    -s 'config."clientSecret"='"$GOOGLE_SECRET" \
    -s 'config."defaultScope"=openid profile email'
else
  echo "    google IdP already exists"
fi

echo "==> Done. Verify: token audience=provider-portal-bff, realm_access.roles include provider_pending after registration."
