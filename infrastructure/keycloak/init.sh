#!/bin/bash
set -e

KEYCLOAK_URL="${KEYCLOAK_URL:-http://keycloak:8080}"
ADMIN_USER="${KEYCLOAK_ADMIN:-admin}"
ADMIN_PASS="${KEYCLOAK_ADMIN_PASSWORD:-admin}"
REALM="inktavia-realm"
KCADM="/opt/keycloak/bin/kcadm.sh"
FLOW_NAME="Provider OTP Login browser"
FLOW_NAME_ENC="Provider%20OTP%20Login%20browser"

# Host/port for the raw /dev/tcp Keycloak Admin REST calls (see _kc_* helpers below). Derived from KEYCLOAK_URL.
KC_REST_HOST=$(printf '%s' "$KEYCLOAK_URL" | sed -E 's#^https?://##; s#[:/].*$##')
KC_REST_PORT=$(printf '%s' "$KEYCLOAK_URL" | sed -nE 's#^https?://[^:/]+:([0-9]+).*#\1#p'); [ -z "$KC_REST_PORT" ] && KC_REST_PORT=8080

echo "Waiting for Keycloak to be ready..."
until (echo > /dev/tcp/keycloak/9000) 2>/dev/null; do
  echo "Still waiting..."
  sleep 3
done
sleep 5

echo "Keycloak is ready. Configuring master realm SSL..."
$KCADM config credentials \
  --server "${KEYCLOAK_URL}" \
  --realm master \
  --user "${ADMIN_USER}" \
  --password "${ADMIN_PASS}"

$KCADM update realms/master -s sslRequired=none
echo "Master realm sslRequired set to none."

$KCADM update realms/${REALM} -s sslRequired=none 2>/dev/null || true

# ── Realm SMTP parolası (option b: env'den, idempotent) ───────────────────────
# Sır OLMAYAN SMTP alanları realm.json'da duruyor (host/port/user/from/fromDisplayName/replyTo/ssl/starttls/
# auth) ve --import-realm ile geliyor. Parola PUBLIC repoda DURAMAZ; mühürlü secret'tan SMTP_PASSWORD env'i
# olarak gelir ve yalnız burada set edilir. kcadm nested `-s smtpServer.password=...` mevcut smtpServer'ı
# MERGE eder (GET→set→PUT), diğer 9 alan korunur.
# ${...} realm-import ikamesi bu KC sürümünde DOĞRULANMADIĞI için realm.json'a placeholder YAZILMADI — aksi
# halde literal '${...}' parola olarak import edilir ve gönderimde yanıltıcı bir auth hatası verir.
if [ -n "${SMTP_PASSWORD:-}" ]; then
  $KCADM update realms/${REALM} -s "smtpServer.password=${SMTP_PASSWORD}" 2>/dev/null \
    && echo "realm SMTP: password set from SMTP_PASSWORD env." \
    || echo "WARN: realm SMTP password update failed (kcadm)."
  # Geri okuma — parola REDACTED; run log son durumu göstersin (Task B ile aynı fikir).
  echo "  realm SMTP config (password redacted):"
  $KCADM get realms/${REALM} --fields smtpServer 2>/dev/null \
    | tr -d '\n' | sed -E 's/"password"[^,}]*/"password":"<REDACTED>"/' \
    || echo "     (smtpServer okunamadi)"
  echo ""
else
  # ÇALIŞAN bir parolayı boşla EZMEMEK için adımı atlıyoruz — ve GÜRÜLTÜLÜ söylüyoruz.
  echo "WARN: SMTP_PASSWORD unset — realm SMTP password NOT changed (skipping; will not overwrite a working password)."
fi

# ══════════════════════════════════════════════════════════════════════════════
# Shared helpers for the OTP → Keycloak login-ticket handoff wiring.
#
# The Keycloak 25 image has NO python3/jq/curl — only bash + sed + grep + tr + wc + tail + /dev/tcp.
# Two KC25-specific traps this wiring must survive (proven empirically):
#   • kcadm CANNOT read OR write a client's nested `authenticationFlowBindingOverrides` map — every
#     `-s dotted=` / `-b '{...nested...}'` update silently no-ops, and even `get --fields` misreports it.
#     → the client browser-flow binding is done via a raw Keycloak Admin REST API PUT (_kc_bind_browser_flow).
#   • the authenticator config (a FLAT string map) CAN be set by kcadm and must be RE-APPLIED on every run,
#     not only when the flow is first created — otherwise a re-imported realm keeps a stale/empty ticketSecret
#     and the HMAC check fails → Keycloak silently falls back to the password form (_ensure_otp_authenticator).
# ══════════════════════════════════════════════════════════════════════════════

# First "id" value in a kcadm JSON array/object (id is always the leading field).
_first_id() { grep -o '"id"[^,}]*' | head -1 | sed -E 's/.*"([A-Za-z0-9_-]+)".*/\1/'; }
# "id" of the object (within a kcadm array) whose flattened text contains the literal $1.
_id_where() { tr -d '\n' | sed 's/}, *{/}\n{/g' | grep -F "$1" | head -1 | grep -o '"id"[^,}]*' | head -1 | sed -E 's/.*"([A-Za-z0-9_-]+)".*/\1/'; }
# UUID of the top-level auth flow whose alias contains $1. Matches on the alias VALUE (not "alias":"…")
# so it is agnostic to kcadm's pretty-print spacing (`"alias" : "…"`). Flow aliases here are unique substrings.
_flow_id() { $KCADM get authentication/flows -r ${REALM} --fields id,alias 2>/dev/null | tr -d '\n' | sed 's/}, *{/}\n{/g' | grep -F "$1" | head -1 | grep -oE '[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}' | head -1; }

# NEDEN VAR: add-roles'un `|| echo "... skipped"` deseni BAŞARI, ZATEN-VAR ve BAŞARISIZLIK için aynı cümleyi
# basıyor → exit kodu yutuluyordu. marine-mobile-bff bloğu yazıldığından beri identity_read/write + dört
# realm-management rolünü "veriyordu" ama 2026-08-21'de canlı realm'e bakınca servis hesabında HİÇBİRİ yoktu;
# log "skipped (already present or roles absent)" diyordu, kimse ayırt edemedi. Bu yardımcı, her bloktan sonra
# eşlemeyi GERİ OKUYUP servis hesabının SON durumunu log'a basar → eksik rol run log'unda GÖRÜNÜR olur.
# (Amaç run'ı DÜŞÜRMEK değil; eksikliği görünür kılmak — init.sh asla abort etmez.)
_sa_roles() {   # $1 = service-account user id, $2 = etiket
  echo "  $2 — servis hesabinin SON durumu:"
  $KCADM get "users/$1/role-mappings" -r ${REALM} 2>/dev/null \
    | tr -d '\n' | sed 's/}, *{/}\n{/g' \
    | grep -o '"name" *: *"[^"]*"' | sed 's/.*: *"\(.*\)"/     \1/' | sort -u \
    || echo "     (rol eslemesi okunamadi)"
}

# A bearer token for the Keycloak Admin REST API (master realm, admin-cli password grant), via /dev/tcp.
_kc_admin_token() {
  local body="grant_type=password&client_id=admin-cli&username=${ADMIN_USER}&password=${ADMIN_PASS}"
  exec 3<>/dev/tcp/${KC_REST_HOST}/${KC_REST_PORT} 2>/dev/null || return 1
  printf 'POST /realms/master/protocol/openid-connect/token HTTP/1.1\r\nHost: %s:%s\r\nContent-Type: application/x-www-form-urlencoded\r\nContent-Length: %s\r\nConnection: close\r\n\r\n%s' \
    "${KC_REST_HOST}" "${KC_REST_PORT}" "${#body}" "$body" >&3
  local resp; resp=$(cat <&3); exec 3>&- 3<&- || true
  printf '%s' "$resp" | tr ',' '\n' | grep -o '"access_token":"[^"]*"' | head -1 | sed -E 's/.*:"([^"]*)"/\1/'
}

# Raw Admin REST GET → write the response body (exactly Content-Length bytes; body is the final CL bytes
# because Connection: close) to file $3. Byte-exact: no header parsing, no newline munging.
# $1=token $2=path $3=outfile  → returns 0 on success.
_kc_rest_get() {
  local tok="$1" path="$2" out="$3" rawf="${3}.raw"
  exec 3<>/dev/tcp/${KC_REST_HOST}/${KC_REST_PORT} 2>/dev/null || return 1
  printf 'GET %s HTTP/1.1\r\nHost: %s:%s\r\nAuthorization: Bearer %s\r\nAccept: application/json\r\nConnection: close\r\n\r\n' \
    "$path" "${KC_REST_HOST}" "${KC_REST_PORT}" "$tok" >&3
  cat <&3 > "$rawf"; exec 3>&- 3<&- || true
  local cl; cl=$(tr -d '\r' < "$rawf" | grep -i '^content-length:' | head -1 | sed -E 's/[^0-9]//g')
  [ -z "$cl" ] && { rm -f "$rawf"; return 1; }
  tail -c "$cl" "$rawf" > "$out"; rm -f "$rawf"
}

# Raw Admin REST PUT of the JSON in file $3. Prints the status line. $1=token $2=path $3=bodyfile.
_kc_rest_put() {
  local tok="$1" path="$2" bodyf="$3" len; len=$(wc -c < "$bodyf")
  exec 3<>/dev/tcp/${KC_REST_HOST}/${KC_REST_PORT} 2>/dev/null || return 1
  printf 'PUT %s HTTP/1.1\r\nHost: %s:%s\r\nAuthorization: Bearer %s\r\nContent-Type: application/json\r\nContent-Length: %s\r\nConnection: close\r\n\r\n' \
    "$path" "${KC_REST_HOST}" "${KC_REST_PORT}" "$tok" "$len" >&3
  cat "$bodyf" >&3
  local pr; pr=$(cat <&3); exec 3>&- 3<&- || true
  printf '%s' "$pr" | head -1 | tr -d '\r'
}

# BUG-2 FIX — set a client's browser flow binding via the Admin REST API (kcadm can't on KC25).
# GET the full client rep (preserving every other field), splice authenticationFlowBindingOverrides.browser,
# PUT it back, then GET again and ASSERT the flow id took. $1=clientId(name) $2=flowId $3=label.
_kc_bind_browser_flow() {
  local cname="$1" fid="$2" label="$3"
  if [ -z "$fid" ]; then echo "  WARN: [${label}] no flow id — binding skipped."; return 1; fi
  local tok; tok=$(_kc_admin_token)
  if [ -z "$tok" ]; then echo "  WARN: [${label}] could not obtain admin REST token — binding NOT applied."; return 1; fi
  local cuuid; cuuid=$($KCADM get clients -r ${REALM} -q clientId=${cname} --fields id 2>/dev/null | _first_id)
  if [ -z "$cuuid" ]; then echo "  WARN: [${label}] client '${cname}' not found — binding skipped."; return 1; fi

  local bf="/tmp/kc_${cname}.json"
  _kc_rest_get "$tok" "/admin/realms/${REALM}/clients/${cuuid}" "$bf" || { echo "  WARN: [${label}] REST GET of client failed."; return 1; }
  local before; before=$(grep -o '"authenticationFlowBindingOverrides":{[^}]*}' "$bf")

  if grep -q '"authenticationFlowBindingOverrides"' "$bf"; then
    sed -E "s/\"authenticationFlowBindingOverrides\":\{[^}]*\}/\"authenticationFlowBindingOverrides\":{\"browser\":\"${fid}\"}/" "$bf" | tr -d '\n' > "${bf}.new"
  else
    # field absent → inject right after the opening brace of the client object
    sed -E "s/^\{/{\"authenticationFlowBindingOverrides\":{\"browser\":\"${fid}\"},/" "$bf" | tr -d '\n' > "${bf}.new"
  fi

  local status; status=$(_kc_rest_put "$tok" "/admin/realms/${REALM}/clients/${cuuid}" "${bf}.new")
  # Verify via REST (kcadm can't read this field reliably).
  local after=""
  if _kc_rest_get "$tok" "/admin/realms/${REALM}/clients/${cuuid}" "${bf}.chk"; then
    after=$(grep -o '"authenticationFlowBindingOverrides":{[^}]*}' "${bf}.chk")
  fi
  rm -f "$bf" "${bf}.new" "${bf}.chk"

  if printf '%s' "$after" | grep -q "$fid"; then
    echo "  [${label}] browser-flow binding applied via REST PUT (${status}): before=${before:-<none>} → after=${after} ✓"
  else
    echo "  WARN: [${label}] binding did NOT take (PUT ${status}; before=${before:-<none>}; after=${after:-<none>})."
  fi
}

# BUG-1 FIX — ensure a "<X> OTP Login browser" flow has the provider-otp-login-ticket authenticator,
# ALTERNATIVE + raised to the top, with its config (re)applied EVERY run (idempotent overwrite of the
# secret — this is the self-heal). Works whether the flow was just created or pre-existed (realm re-import).
# $1=flowName $2=flowNameEnc $3=configAlias $4=consumePath $5=allowedClientId.
_ensure_otp_authenticator() {
  local fn="$1" fe="$2" alias="$3" cpath="$4" cid="$5"

  local exists; exists=$($KCADM get authentication/flows -r ${REALM} --fields alias 2>/dev/null | grep -c "\"${fn}\"" || true)
  if [ "$exists" -eq 0 ]; then
    $KCADM create authentication/flows/browser/copy -r ${REALM} -s "newName=${fn}" 2>/dev/null || true
    echo "  [${fn}] flow created (copy of browser)."
  else
    echo "  [${fn}] flow already exists — re-applying authenticator config (idempotent self-heal)."
  fi

  # Ensure the custom execution exists in the flow.
  local execid; execid=$($KCADM get "authentication/flows/${fe}/executions" -r ${REALM} 2>/dev/null | _id_where "provider-otp-login-ticket")
  if [ -z "$execid" ]; then
    $KCADM create "authentication/flows/${fe}/executions/execution" -r ${REALM} -s "provider=provider-otp-login-ticket" -o 2>/dev/null || true
    execid=$($KCADM get "authentication/flows/${fe}/executions" -r ${REALM} 2>/dev/null | _id_where "provider-otp-login-ticket")
  fi
  if [ -z "$execid" ]; then echo "  WARN: [${fn}] could not resolve execution id — authenticator NOT configured."; return 1; fi

  # ALTERNATIVE + raise to top (idempotent — a no-op once already at index 0).
  $KCADM update "authentication/flows/${fe}/executions" -r ${REALM} -b "{\"id\":\"${execid}\",\"requirement\":\"ALTERNATIVE\"}" 2>/dev/null || true
  for i in 1 2 3 4 5; do $KCADM create "authentication/executions/${execid}/raise-priority" -r ${REALM} 2>/dev/null || true; done

  # (Re)apply the config. Update the existing config id if present, else create it. ALWAYS overwrites the
  # secret so a stale value from a previously-imported realm is corrected.
  local cfgid; cfgid=$($KCADM get "authentication/flows/${fe}/executions" -r ${REALM} 2>/dev/null | tr -d '\n' | sed 's/}, *{/}\n{/g' | grep -F 'provider-otp-login-ticket' | head -1 | grep -o '"authenticationConfig"[^,}]*' | grep -oE '[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}' | head -1)
  local masked="${OTP_LOGIN_TICKET_SECRET:0:6}…(len=${#OTP_LOGIN_TICKET_SECRET})"
  if [ -n "$cfgid" ]; then
    $KCADM update "authentication/config/${cfgid}" -r ${REALM} \
      -b "{\"id\":\"${cfgid}\",\"alias\":\"${alias}\",\"config\":{\"identityBaseUrl\":\"http://identity-api:8080\",\"consumePath\":\"${cpath}\",\"ticketSecret\":\"${OTP_LOGIN_TICKET_SECRET}\",\"consumeSecret\":\"${OTP_LOGIN_CONSUME_SECRET}\",\"allowedClientId\":\"${cid}\",\"maxSkewSeconds\":\"30\"}}" 2>/dev/null \
      && echo "  [${fn}] authenticator config UPDATED (id=${cfgid}): ticketSecret=${masked} consumePath=${cpath} allowedClientId=${cid}." \
      || echo "  WARN: [${fn}] authenticator config update FAILED (id=${cfgid})."
  else
    $KCADM create "authentication/executions/${execid}/config" -r ${REALM} \
      -s "alias=${alias}" \
      -s "config.identityBaseUrl=http://identity-api:8080" \
      -s "config.consumePath=${cpath}" \
      -s "config.ticketSecret=${OTP_LOGIN_TICKET_SECRET}" \
      -s "config.consumeSecret=${OTP_LOGIN_CONSUME_SECRET}" \
      -s "config.allowedClientId=${cid}" \
      -s "config.maxSkewSeconds=30" 2>/dev/null \
      && echo "  [${fn}] authenticator config CREATED: ticketSecret=${masked} consumePath=${cpath} allowedClientId=${cid}." \
      || echo "  WARN: [${fn}] authenticator config create FAILED."
  fi
}

# ── Provider OTP Login authenticator flow ──────────────────────────────────────
if [ -n "${OTP_LOGIN_TICKET_SECRET:-}" ] && [ -n "${OTP_LOGIN_CONSUME_SECRET:-}" ]; then
  echo "Setting up Provider OTP Login browser flow (idempotent)..."
  _ensure_otp_authenticator "${FLOW_NAME}" "${FLOW_NAME_ENC}" \
    "provider-otp-login-config" "/api/v1/identity/auth/provider-otp-login/consume-ticket" "provider-portal" || true
  _kc_bind_browser_flow "provider-portal" "$(_flow_id "${FLOW_NAME}")" "provider-portal→Provider OTP flow" || true
else
  echo "OTP_LOGIN_TICKET_SECRET or OTP_LOGIN_CONSUME_SECRET not set, skipping OTP login flow setup."
fi

# ── Admin OTP Login authenticator flow ─────────────────────────────────────────
ADMIN_FLOW_NAME="Admin OTP Login browser"
ADMIN_FLOW_NAME_ENC="Admin%20OTP%20Login%20browser"
ADMIN_CLIENT_ID="admin-panel"
ADMIN_EMAIL="admin.user@inktavia.com"

if [ -n "${OTP_LOGIN_TICKET_SECRET:-}" ] && [ -n "${OTP_LOGIN_CONSUME_SECRET:-}" ]; then
  echo "Setting up Admin OTP Login browser flow (idempotent)..."

  # 1) Ensure the 'Admin' realm role exists and assign it to the admin user.
  #    (Realm ships 'admin_user' for API scopes; the .NET/FE side requires the 'Admin' role — keep both.)
  $KCADM get "roles/Admin" -r ${REALM} >/dev/null 2>&1 || \
    $KCADM create roles -r ${REALM} -s name=Admin -s "description=Admin panel access" 2>/dev/null || true
  $KCADM add-roles -r ${REALM} --uusername "${ADMIN_EMAIL}" --rolename Admin 2>/dev/null || true
  echo "Ensured 'Admin' realm role and assigned it to ${ADMIN_EMAIL}."

  # 2) Fix the admin-panel client for the code handoff: enable it, turn on the standard (code) flow,
  #    and add the localhost:3000 dev redirect/origin (keeping the existing 3001/prod entries).
  ADMIN_CLIENT_UUID=$($KCADM get clients -r ${REALM} -q clientId=${ADMIN_CLIENT_ID} --fields id 2>/dev/null | _first_id)

  if [ -n "$ADMIN_CLIENT_UUID" ]; then
    $KCADM update "clients/${ADMIN_CLIENT_UUID}" -r ${REALM} -b '{"enabled":true,"standardFlowEnabled":true,"publicClient":true,"redirectUris":["http://localhost:3000/*","http://localhost:3001/*","https://admin.inktavia.com/*"],"webOrigins":["http://localhost:3000","http://localhost:3001","https://admin.inktavia.com"]}' 2>/dev/null || true
    echo "admin-panel client enabled + standardFlow + localhost:3000 redirect/origin applied."

    # 2b) Ensure the admin-panel access token carries aud: admin-panel-bff. bff-adminpanel validates
    #     ValidAudience=admin-panel-bff, so without this the FE token 401s at authentication (Kickoff 2d).
    #     Mirrors the provider-portal -> provider-portal-bff audience pattern. Idempotent (create once).
    HAS_AUD_MAPPER=$($KCADM get "clients/${ADMIN_CLIENT_UUID}/protocol-mappers/models" -r ${REALM} 2>/dev/null | grep -c '"audience-admin-panel-bff"' || true)
    if [ "$HAS_AUD_MAPPER" -eq 0 ]; then
      $KCADM create "clients/${ADMIN_CLIENT_UUID}/protocol-mappers/models" -r ${REALM} \
        -s name=audience-admin-panel-bff \
        -s protocol=openid-connect \
        -s protocolMapper=oidc-audience-mapper \
        -s 'config."included.client.audience"=admin-panel-bff' \
        -s 'config."id.token.claim"=false' \
        -s 'config."access.token.claim"=true' 2>/dev/null || true
      echo "admin-panel: added audience mapper (access token aud += admin-panel-bff)."
    else
      echo "admin-panel: audience mapper 'audience-admin-panel-bff' already present."
    fi
  else
    echo "WARN: admin-panel client not found — skipping client fix."
  fi

  # 3) Real-subject linkage: resolve the admin user's real Keycloak subject id and POST it to the Identity
  #    admin-provision endpoint (server-to-server, shared consume-secret header). This replaces the
  #    Kickoff-1 seed placeholder so the SPI can resolve the user at handoff. Uses a raw /dev/tcp HTTP
  #    request (image has no curl); the secret is passed as a header only and never echoed.
  ADMIN_USER_UUID=$($KCADM get users -r ${REALM} -q email=${ADMIN_EMAIL} --fields id 2>/dev/null | _first_id)

  if [ -n "$ADMIN_USER_UUID" ]; then
    PROV_BODY="{\"keycloakSubjectId\":\"${ADMIN_USER_UUID}\",\"email\":\"${ADMIN_EMAIL}\",\"firstName\":\"Admin\",\"lastName\":\"User\",\"emailVerified\":true}"
    PROV_LEN=${#PROV_BODY}
    if exec 3<>/dev/tcp/identity-api/8080 2>/dev/null; then
      printf 'POST /api/v1/identity/auth/admin-otp-login/admin-provision HTTP/1.1\r\nHost: identity-api:8080\r\nContent-Type: application/json\r\nX-Otp-Login-Consume-Secret: %s\r\nContent-Length: %s\r\nConnection: close\r\n\r\n%s' \
        "${OTP_LOGIN_CONSUME_SECRET}" "${PROV_LEN}" "${PROV_BODY}" >&3
      PROV_STATUS=$(head -1 <&3 | tr -d '\r')
      exec 3>&- 3<&-
      echo "Admin real-subject linkage (KC subject -> Identity): ${PROV_STATUS}"
    else
      echo "WARN: could not connect to identity-api:8080 for admin-provision (is identity-api up?)."
    fi
  else
    echo "WARN: admin user ${ADMIN_EMAIL} not found — skipping real-subject linkage."
  fi

  # 4) Ensure the "Admin OTP Login browser" flow + authenticator config — ALWAYS (idempotent self-heal),
  #    pointing at the ADMIN consume path. Fixes BUG-1 (config used to be set only on the create branch).
  _ensure_otp_authenticator "${ADMIN_FLOW_NAME}" "${ADMIN_FLOW_NAME_ENC}" \
    "admin-otp-login-config" "/api/v1/identity/auth/admin-otp-login/consume-ticket" "${ADMIN_CLIENT_ID}" || true

  # 5) Bind admin-panel to the admin flow via the Admin REST API — ALWAYS (idempotent). Fixes BUG-2
  #    (the kcadm nested-map binding silently no-ops on KC25, so admin-panel used the DEFAULT browser flow).
  _kc_bind_browser_flow "${ADMIN_CLIENT_ID}" "$(_flow_id "${ADMIN_FLOW_NAME}")" "admin-panel→Admin OTP flow" || true
else
  echo "OTP_LOGIN_TICKET_SECRET or OTP_LOGIN_CONSUME_SECRET not set, skipping Admin OTP login flow setup."
fi

# ── Participant (mobile) OTP Login flow + durable marine-mobile-bff client (mirrors the admin block) ──
# Same kcadm + sed/grep approach (KC25 image: no python3/jq/curl). Three additive, idempotent parts:
#   A) durable marine-mobile-bff confidential client (create-if-absent) — a running Keycloak won't
#      re-import the seed JSON, so recreate it here so the mobile BFF has a real client to authenticate.
#   B) audience-marine-mobile-bff mapper on inktavia-mobile (add-if-missing) — so participant access
#      tokens carry aud: marine-mobile-bff (what the mobile BFF validates).
#   C) the "Participant OTP Login browser" flow bound to inktavia-mobile (guarded by the OTP secrets).
PART_FLOW_NAME="Participant OTP Login browser"
PART_FLOW_NAME_ENC="Participant%20OTP%20Login%20browser"
MOBILE_CLIENT_ID="inktavia-mobile"
MM_BFF_CLIENT_ID="marine-mobile-bff"
MM_BFF_SECRET="${KEYCLOAK_MARINE_MOBILE_BFF_CLIENT_SECRET:-local-dev-only-change-me}"

# A) Durable marine-mobile-bff confidential client (create-if-absent), mirroring admin-panel-bff:
#    confidential, service accounts on, standard/direct-grant off, per-module oidc-audience-mappers.
MM_BFF_UUID=$($KCADM get clients -r ${REALM} -q clientId=${MM_BFF_CLIENT_ID} --fields id 2>/dev/null | _first_id)
if [ -z "$MM_BFF_UUID" ]; then
  $KCADM create clients -r ${REALM} \
    -b "{\"clientId\":\"${MM_BFF_CLIENT_ID}\",\"enabled\":true,\"protocol\":\"openid-connect\",\"publicClient\":false,\"serviceAccountsEnabled\":true,\"standardFlowEnabled\":false,\"directAccessGrantsEnabled\":false,\"secret\":\"${MM_BFF_SECRET}\"}" 2>/dev/null || true
  MM_BFF_UUID=$($KCADM get clients -r ${REALM} -q clientId=${MM_BFF_CLIENT_ID} --fields id 2>/dev/null | _first_id)
  if [ -n "$MM_BFF_UUID" ]; then
    # Audience mappers for every module the mobile BFF fronts (matches inktavia-mobile's set).
    for AUD in identity-api profile-api vessel-api file-storage-api service-request-api reference-data-api cargodry-api messaging-api notification-api; do
      $KCADM create "clients/${MM_BFF_UUID}/protocol-mappers/models" -r ${REALM} \
        -s "name=audience-${AUD}" \
        -s protocol=openid-connect \
        -s protocolMapper=oidc-audience-mapper \
        -s "config.\"included.client.audience\"=${AUD}" \
        -s 'config."id.token.claim"=false' \
        -s 'config."access.token.claim"=true' 2>/dev/null || true
    done
    echo "marine-mobile-bff: created (confidential, serviceAccounts) with module audience mappers."
  else
    echo "WARN: marine-mobile-bff create attempted but UUID not resolved."
  fi
else
  echo "marine-mobile-bff client already exists, skipping creation."
fi

# Resolve the marine-mobile-bff SERVICE ACCOUNT user id. NOTE: kcadm `--uusername service-account-<client>`
# silently no-ops the realm-role grant in this image (the service-account username isn't resolvable by the
# users search), so all grants below use the resolved user id (`--uid`) which is reliable.
MM_BFF_UUID_FOR_SA=$($KCADM get clients -r ${REALM} -q clientId=${MM_BFF_CLIENT_ID} --fields id 2>/dev/null | _first_id)
MM_BFF_SA_UID=$($KCADM get "clients/${MM_BFF_UUID_FOR_SA}/service-account-user" -r ${REALM} --fields id 2>/dev/null | _first_id)

# A2) Grant the marine-mobile-bff SERVICE ACCOUNT the Identity API scopes it needs for BFF→Identity calls
#     (participant OTP-login / provisioning are IdentityWrite). Idempotent (re-adding an existing role is a
#     no-op). Runs always so a previously bare-created client (M2b) is repaired, not only on first creation.
if [ -n "$MM_BFF_SA_UID" ]; then
  $KCADM add-roles -r ${REALM} --uid "$MM_BFF_SA_UID" \
    --rolename identity_read --rolename identity_write 2>/dev/null \
    && echo "marine-mobile-bff: service account granted identity_read + identity_write." \
    || echo "marine-mobile-bff: identity_read/identity_write grant skipped (already present or roles absent)."

  # A3) Realm-management roles the mobile BFF needs for the Keycloak Admin API (register: create-user /
  #     set-attribute / assign-role / send-verify-email; find/get user; read realm role for the mapping).
  #     view-realm is required to GET a realm role by name before mapping it. Mirrors the provider BFF SA
  #     minus the broad realm-admin (least privilege). Idempotent.
  $KCADM add-roles -r ${REALM} --uid "$MM_BFF_SA_UID" \
    --cclientid realm-management \
    --rolename manage-users --rolename view-users --rolename query-users --rolename view-realm 2>/dev/null \
    && echo "marine-mobile-bff: service account granted realm-management manage-users/view-users/query-users/view-realm." \
    || echo "marine-mobile-bff: realm-management grant skipped (already present)."

  _sa_roles "$MM_BFF_SA_UID" "marine-mobile-bff"
else
  echo "WARN: could not resolve marine-mobile-bff service-account user id — role grants skipped."
fi

# A4) Enable directAccessGrants on the CONFIDENTIAL marine-mobile-bff so the BFF can verify passwords
#     server-side (ROPC), take the sub, and discard the token. ROPC is NOT enabled on public inktavia-mobile.
if [ -n "$MM_BFF_UUID_FOR_SA" ]; then
  $KCADM update "clients/${MM_BFF_UUID_FOR_SA}" -r ${REALM} \
    -b '{"directAccessGrantsEnabled":true}' 2>/dev/null \
    && echo "marine-mobile-bff: directAccessGrantsEnabled=true (server-side password verification)." \
    || echo "marine-mobile-bff: directAccessGrants update skipped."
fi

# B) Ensure inktavia-mobile access tokens carry aud: marine-mobile-bff (add-if-missing).
MOBILE_CLIENT_UUID=$($KCADM get clients -r ${REALM} -q clientId=${MOBILE_CLIENT_ID} --fields id 2>/dev/null | _first_id)
if [ -n "$MOBILE_CLIENT_UUID" ]; then
  HAS_MM_AUD_MAPPER=$($KCADM get "clients/${MOBILE_CLIENT_UUID}/protocol-mappers/models" -r ${REALM} 2>/dev/null | grep -c '"audience-marine-mobile-bff"' || true)
  if [ "$HAS_MM_AUD_MAPPER" -eq 0 ]; then
    $KCADM create "clients/${MOBILE_CLIENT_UUID}/protocol-mappers/models" -r ${REALM} \
      -s name=audience-marine-mobile-bff \
      -s protocol=openid-connect \
      -s protocolMapper=oidc-audience-mapper \
      -s 'config."included.client.audience"=marine-mobile-bff' \
      -s 'config."id.token.claim"=false' \
      -s 'config."access.token.claim"=true' 2>/dev/null || true
    echo "inktavia-mobile: added audience mapper (access token aud += marine-mobile-bff)."
  else
    echo "inktavia-mobile: audience mapper 'audience-marine-mobile-bff' already present."
  fi

  # B2) Register the mobile BFF handoff redirect_uri on inktavia-mobile (add-if-missing). The BFF's
  #     native ticket→session handoff (M2c) drives auth-code+PKCE and presents this redirect_uri; Keycloak
  #     validates it against the client's registered list. Setting the array replaces it, so we write the
  #     union of the seed URIs + the BFF one, only when the BFF one is absent (idempotent).
  MM_BFF_REDIRECT="${MARINE_MOBILE_BFF_REDIRECT_URI:-http://localhost:17003/auth/callback}"
  HAS_BFF_REDIRECT=$($KCADM get "clients/${MOBILE_CLIENT_UUID}" -r ${REALM} 2>/dev/null | tr -d '\n' | grep -c "${MM_BFF_REDIRECT}" || true)
  if [ "$HAS_BFF_REDIRECT" -eq 0 ]; then
    $KCADM update "clients/${MOBILE_CLIENT_UUID}" -r ${REALM} \
      -b "{\"redirectUris\":[\"inktavia://auth/callback\",\"com.inktavia.mobile://auth/callback\",\"http://localhost:19006/auth/callback\",\"${MM_BFF_REDIRECT}\"]}" 2>/dev/null || true
    echo "inktavia-mobile: registered BFF handoff redirect_uri ${MM_BFF_REDIRECT}."
  else
    echo "inktavia-mobile: BFF handoff redirect_uri already registered."
  fi
else
  echo "WARN: inktavia-mobile client not found — skipping audience mapper + BFF redirect."
fi

# C) Participant OTP Login browser flow + bind inktavia-mobile (guarded by the OTP secrets).
if [ -n "${OTP_LOGIN_TICKET_SECRET:-}" ] && [ -n "${OTP_LOGIN_CONSUME_SECRET:-}" ]; then
  echo "Setting up Participant OTP Login browser flow (idempotent)..."
  _ensure_otp_authenticator "${PART_FLOW_NAME}" "${PART_FLOW_NAME_ENC}" \
    "participant-otp-login-config" "/api/v1/identity/auth/participant-otp-login/consume-ticket" "${MOBILE_CLIENT_ID}" || true
  _kc_bind_browser_flow "${MOBILE_CLIENT_ID}" "$(_flow_id "${PART_FLOW_NAME}")" "inktavia-mobile→Participant OTP flow" || true
else
  echo "OTP_LOGIN_TICKET_SECRET or OTP_LOGIN_CONSUME_SECRET not set, skipping Participant OTP login flow setup."
fi

# ── FAZ18 (#73 / #74 / #25) — dev host redirect'lerini ÇALIŞAN realm'e idempotent uygula ──────────────
# ⚠️ DEV/PROD TEK REALM, TEK İSTEMCİ (#25): Ortamda TEK Keycloak ve TEK realm var; 22 modül/BFF values dosyası
# AYNI Keycloak__Authority'yi taşır. Bu yüzden bir istemciye 'dev-*' redirect_uri eklemek, PROD'un da kullandığı
# AYNI istemciye ekler. SOMUT SONUÇ: bu dev host'unu KİM KONTROL EDİYORSA, o istemci için authorization code
# ALABİLİR. Host'lar BİZİM olduğundan bugün kabul edilebilir; yarın (host el değiştirir / başka ekibe geçer) DEĞİL.
# Bunlar tam olarak #25 (dev/prod realm ayrımı) geldiğinde AYRILMASI gereken girişlerdir; ayrımı BURADA yapmıyoruz.
# --import-realm mevcut bir realm'i YENİDEN import ETMEZ → realm.json'daki bu redirect'ler yalnız SIFIRDAN import'ta
# gelir; çalışan realm'e ancak bu idempotent adım ulaşır (init.sh'in varlık nedeni, audience mapper create-if-absent
# ile aynı desen). NOT: inktavia-mobile B2 bloğu redirectUris'i MARINE_MOBILE_BFF_REDIRECT_URI ile yönetir; bu blok
# B2'DEN SONRA çalışıp dev-mapi'yi (üzerine yazmadan) EKLER — o yüzden çakışmaz.

# provider-portal — dev host (#73: canlıya elle eklenmişti, repo'da yoktu; artık realm.json'da + burada)
PP_UUID=$($KCADM get clients -r ${REALM} -q clientId=provider-portal --fields id 2>/dev/null | _first_id)
if [ -n "$PP_UUID" ]; then
  HAS_PP_DEV=$($KCADM get "clients/${PP_UUID}" -r ${REALM} 2>/dev/null | tr -d '\n' | grep -c "dev-provider.inktavia.com" || true)
  if [ "$HAS_PP_DEV" -eq 0 ]; then
    $KCADM update "clients/${PP_UUID}" -r ${REALM} \
      -s 'redirectUris+=https://dev-provider.inktavia.com/*' \
      -s 'webOrigins+=https://dev-provider.inktavia.com' 2>/dev/null \
      && echo "provider-portal: dev host redirect/webOrigin eklendi (dev-provider.inktavia.com)." \
      || echo "  WARN: provider-portal dev host eklenemedi."
  else
    echo "provider-portal: dev host zaten kayıtlı."
  fi
else
  echo "WARN: provider-portal istemcisi bulunamadı — dev host atlandı."
fi

# inktavia-mobile — dev BFF handoff redirect (#74: repo & canlıda eksikti; dev mobil OTP login bunsuz Keycloak
# authorize adımında reddediliyordu). App dev'de MarineMobileKeycloak__BffRedirectUri=dev-mapi (values-dev) ile
# gidiyor; bu URI istemcide kayıtlı OLMALI.
MOBILE_UUID=$($KCADM get clients -r ${REALM} -q clientId=inktavia-mobile --fields id 2>/dev/null | _first_id)
if [ -n "$MOBILE_UUID" ]; then
  HAS_M_DEV=$($KCADM get "clients/${MOBILE_UUID}" -r ${REALM} 2>/dev/null | tr -d '\n' | grep -c "dev-mapi.inktavia.com/auth/callback" || true)
  if [ "$HAS_M_DEV" -eq 0 ]; then
    $KCADM update "clients/${MOBILE_UUID}" -r ${REALM} \
      -s 'redirectUris+=https://dev-mapi.inktavia.com/auth/callback' 2>/dev/null \
      && echo "inktavia-mobile: dev redirect_uri eklendi (dev-mapi.inktavia.com/auth/callback)." \
      || echo "  WARN: inktavia-mobile dev redirect_uri eklenemedi."
  else
    echo "inktavia-mobile: dev redirect_uri zaten kayıtlı."
  fi
else
  echo "WARN: inktavia-mobile istemcisi bulunamadı — dev redirect atlandı."
fi

# ── Marine.Web BFF (public website) + inktavia-web SPA (mirrors the marine-mobile-bff block) ──────────
#   A) durable marine-web-bff confidential service-account client (create-if-absent). Boot-critical: the BFF's
#      delegating handler ALWAYS obtains a service token, so this client must exist or the BFF fails on boot.
#   A2) grant its service account reference_data_read + identity_read (BFF → module reads).
#   B) web_user realm role + the public inktavia-web SPA client (Auth Code + PKCE) with the participant_profile_id
#      mapper — for the FROZEN /me surface only (the website itself never authenticates).
WEB_BFF_CLIENT_ID="marine-web-bff"
WEB_CLIENT_ID="inktavia-web"
WEB_BFF_SECRET="${KEYCLOAK_MARINE_WEB_BFF_CLIENT_SECRET:-local-dev-only-change-me}"
WEB_BASE="${MARINE_WEB_BASE:-http://localhost:3000}"

# A) Durable marine-web-bff confidential client (create-if-absent).
WEB_BFF_UUID=$($KCADM get clients -r ${REALM} -q clientId=${WEB_BFF_CLIENT_ID} --fields id 2>/dev/null | _first_id)
if [ -z "$WEB_BFF_UUID" ]; then
  $KCADM create clients -r ${REALM} \
    -b "{\"clientId\":\"${WEB_BFF_CLIENT_ID}\",\"enabled\":true,\"protocol\":\"openid-connect\",\"publicClient\":false,\"serviceAccountsEnabled\":true,\"standardFlowEnabled\":false,\"directAccessGrantsEnabled\":false,\"secret\":\"${WEB_BFF_SECRET}\"}" 2>/dev/null || true
  WEB_BFF_UUID=$($KCADM get clients -r ${REALM} -q clientId=${WEB_BFF_CLIENT_ID} --fields id 2>/dev/null | _first_id)
  if [ -n "$WEB_BFF_UUID" ]; then
    for AUD in content-api identity-api reference-data-api payment-api notification-api; do
      $KCADM create "clients/${WEB_BFF_UUID}/protocol-mappers/models" -r ${REALM} \
        -s "name=audience-${AUD}" \
        -s protocol=openid-connect \
        -s protocolMapper=oidc-audience-mapper \
        -s "config.\"included.client.audience\"=${AUD}" \
        -s 'config."id.token.claim"=false' \
        -s 'config."access.token.claim"=true' 2>/dev/null || true
    done
    echo "marine-web-bff: created (confidential, serviceAccounts) with module audience mappers."
  else
    echo "WARN: marine-web-bff create attempted but UUID not resolved."
  fi
else
  echo "marine-web-bff client already exists, skipping creation."
fi

# A2) Grant the marine-web-bff SERVICE ACCOUNT the read scopes it needs (reference_data_read for lookups,
#     identity_read for by-subject resolution). Idempotent; runs always so a bare-created client is repaired.
WEB_BFF_UUID_FOR_SA=$($KCADM get clients -r ${REALM} -q clientId=${WEB_BFF_CLIENT_ID} --fields id 2>/dev/null | _first_id)
WEB_BFF_SA_UID=$($KCADM get "clients/${WEB_BFF_UUID_FOR_SA}/service-account-user" -r ${REALM} --fields id 2>/dev/null | _first_id)
if [ -n "$WEB_BFF_SA_UID" ]; then
  $KCADM add-roles -r ${REALM} --uid "$WEB_BFF_SA_UID" \
    --rolename reference_data_read --rolename identity_read 2>/dev/null \
    && echo "marine-web-bff: service account granted reference_data_read + identity_read." \
    || echo "marine-web-bff: reference_data_read/identity_read grant skipped (already present or roles absent)."

  _sa_roles "$WEB_BFF_SA_UID" "marine-web-bff"
else
  echo "WARN: could not resolve marine-web-bff service-account user id — role grants skipped."
fi

# B) web_user realm role (create-if-absent) for the frozen /me surface.
$KCADM create roles -r ${REALM} -s name=web_user 2>/dev/null \
  && echo "Created 'web_user' realm role." \
  || echo "'web_user' realm role already exists (or create skipped)."

# B2) Public inktavia-web SPA client (Auth Code + PKCE), create-if-absent, with the participant_profile_id + audience
#     mappers. Only exercised by the frozen /me surface — the public website never authenticates.
WEB_CLIENT_UUID=$($KCADM get clients -r ${REALM} -q clientId=${WEB_CLIENT_ID} --fields id 2>/dev/null | _first_id)
if [ -z "$WEB_CLIENT_UUID" ]; then
  $KCADM create clients -r ${REALM} \
    -b "{\"clientId\":\"${WEB_CLIENT_ID}\",\"enabled\":true,\"protocol\":\"openid-connect\",\"publicClient\":true,\"standardFlowEnabled\":true,\"directAccessGrantsEnabled\":false,\"redirectUris\":[\"${WEB_BASE}/*\"],\"webOrigins\":[\"${WEB_BASE}\"]}" 2>/dev/null || true
  WEB_CLIENT_UUID=$($KCADM get clients -r ${REALM} -q clientId=${WEB_CLIENT_ID} --fields id 2>/dev/null | _first_id)
  if [ -n "$WEB_CLIENT_UUID" ]; then
    $KCADM create "clients/${WEB_CLIENT_UUID}/protocol-mappers/models" -r ${REALM} \
      -s name=participant_profile_id \
      -s protocol=openid-connect \
      -s protocolMapper=oidc-usermodel-attribute-mapper \
      -s 'config."user.attribute"=participant_profile_id' \
      -s 'config."claim.name"=participant_profile_id' \
      -s 'config."jsonType.label"=String' \
      -s 'config."id.token.claim"=true' \
      -s 'config."access.token.claim"=true' 2>/dev/null || true
    $KCADM create "clients/${WEB_CLIENT_UUID}/protocol-mappers/models" -r ${REALM} \
      -s name=audience-marine-web-bff \
      -s protocol=openid-connect \
      -s protocolMapper=oidc-audience-mapper \
      -s 'config."included.client.audience"=marine-web-bff' \
      -s 'config."id.token.claim"=false' \
      -s 'config."access.token.claim"=true' 2>/dev/null || true
    echo "inktavia-web: created (public, Auth Code + PKCE) with participant_profile_id + audience mappers."
  fi
else
  echo "inktavia-web client already exists, skipping creation."
fi

# ══════════════════════════════════════════════════════════════════════════════
# Servis hesabı rol EŞLEMELERİ — realm.json'a KONULAMAZ, init.sh'e AİT.
#
# NEDEN burada, realm export'unda değil: inktavia-realm-realm.json `users: []` taşıyor.
# Servis hesabı rol eşlemeleri `service-account-<clientId>` KULLANICISININ üzerinde durur; export hiç
# kullanıcı taşımadığı için bu eşlemelerin realm JSON'da yeri yoktur. --import-realm rol TANIMLARINI kurar
# ama hiçbir servis hesabına bir şey EŞLEMEZ. Aşağısı bu boşluğu idempotent kapatır.
#
# provider-portal-bff ve admin-panel-bff init.sh tarafından OLUŞTURULMAZ (zaten realm'de var); yalnız
# servis-hesabı uid'i çözülüp roller eklenir. add-roles idempotenttir (var olan rolü yeniden eklemek
# no-op'tur) ve her çalıştırmada koşar ki elle/eksik kurulmuş bir hesap ONARILSIN.
# ══════════════════════════════════════════════════════════════════════════════

# provider-portal-bff: Identity bunu IdentityKeycloak__AdminClientId olarak kullanıp Keycloak KULLANICISI
# oluşturur/okur (realm-management) VE provider BFF bununla Identity modülüne okuma+yazma çağrısı yapar
# (identity_read/identity_write). identity_read 2026-08-21'de ELLE verilmişti; hiçbir yerde kayıtlı değildi.
PP_BFF_CLIENT_ID="provider-portal-bff"
PP_BFF_UUID_FOR_SA=$($KCADM get clients -r ${REALM} -q clientId=${PP_BFF_CLIENT_ID} --fields id 2>/dev/null | _first_id)
PP_BFF_SA_UID=$($KCADM get "clients/${PP_BFF_UUID_FOR_SA}/service-account-user" -r ${REALM} --fields id 2>/dev/null | _first_id)
if [ -n "$PP_BFF_SA_UID" ]; then
  $KCADM add-roles -r ${REALM} --uid "$PP_BFF_SA_UID" \
    --rolename identity_read --rolename identity_write 2>/dev/null \
    && echo "provider-portal-bff: service account granted identity_read + identity_write." \
    || echo "provider-portal-bff: identity_read/identity_write grant skipped (already present or roles absent)."

  # realm-management: Keycloak Admin API ile kullanıcı oluşturma/okuma/attribute yazma. view-realm, bir realm
  # rolünü isimle GET edip eşlemeden önce gereklidir. marine-mobile-bff ile aynı küme (provider zaten admin client).
  $KCADM add-roles -r ${REALM} --uid "$PP_BFF_SA_UID" \
    --cclientid realm-management \
    --rolename manage-users --rolename view-users --rolename query-users --rolename view-realm 2>/dev/null \
    && echo "provider-portal-bff: service account granted realm-management manage-users/view-users/query-users/view-realm." \
    || echo "provider-portal-bff: realm-management grant skipped (already present)."

  _sa_roles "$PP_BFF_SA_UID" "provider-portal-bff"
else
  echo "WARN: could not resolve provider-portal-bff service-account user id — role grants skipped."
fi

# admin-panel-bff: CANLI hesap HİÇBİR realm rolü taşımıyor; her modül için o modülün *dotted* client rollerini
# taşıyor. Identity policy'leri identity.read/identity.write/identity.admin gibi DOTTED client rolünü de kabul
# ediyor (BFF servis-hesabı token'larında realm rolü yok — AuthorizationPolicyExtensions.cs:17). Bu blok CANLI
# durumu KODLAR: modül başına client rolleri, realm rolü DEĞİL. `Admin` realm rolü BİLEREK verilmiyor — canlı
# hesapta yok ve vermek [Authorize(Roles="Admin,SuperAdmin")] uçlarını (CargoDry admin/finans/ticari/konsinye...)
# prod'da bir sonraki init.sh'te açardı; "var olanı kodla" fazı var olandan fazlasını sessizce veremez (bkz.
# rapor). realm-management GEREKMEZ: admin-panel Keycloak Admin API'sine DOKUNMAZ (kullanıcı/OTP/parola →
# Identity modülüne proxy), yalnız kendi token'ını yeniler. Roller M3 ölçümünden birebir; her biri realm.json
# roles.client altında zaten TANIMLI.
AP_BFF_CLIENT_ID="admin-panel-bff"
AP_BFF_UUID_FOR_SA=$($KCADM get clients -r ${REALM} -q clientId=${AP_BFF_CLIENT_ID} --fields id 2>/dev/null | _first_id)
AP_BFF_SA_UID=$($KCADM get "clients/${AP_BFF_UUID_FOR_SA}/service-account-user" -r ${REALM} --fields id 2>/dev/null | _first_id)
if [ -n "$AP_BFF_SA_UID" ]; then
  # Her --cclientid bloğu o API client'ının dotted rollerini verir. Idempotent (var olan no-op).
  $KCADM add-roles -r ${REALM} --uid "$AP_BFF_SA_UID" --cclientid cargodry-api \
    --rolename cargodry.admin --rolename cargodry.read --rolename cargodry.write 2>/dev/null \
    && echo "admin-panel-bff: cargodry-api client roles granted." \
    || echo "admin-panel-bff: cargodry-api client role grant skipped (already present or roles absent)."

  $KCADM add-roles -r ${REALM} --uid "$AP_BFF_SA_UID" --cclientid file-storage-api \
    --rolename file.delete --rolename file.read --rolename file.read-url.create \
    --rolename file.upload-url.create --rolename file.visibility.manage --rolename file.write 2>/dev/null \
    && echo "admin-panel-bff: file-storage-api client roles granted." \
    || echo "admin-panel-bff: file-storage-api client role grant skipped (already present or roles absent)."

  $KCADM add-roles -r ${REALM} --uid "$AP_BFF_SA_UID" --cclientid identity-api \
    --rolename identity.admin --rolename identity.auth --rolename identity.profile.approve \
    --rolename identity.profile.read --rolename identity.profile.reject \
    --rolename identity.read --rolename identity.write 2>/dev/null \
    && echo "admin-panel-bff: identity-api client roles granted." \
    || echo "admin-panel-bff: identity-api client role grant skipped (already present or roles absent)."

  $KCADM add-roles -r ${REALM} --uid "$AP_BFF_SA_UID" --cclientid messaging-api \
    --rolename messaging.admin --rolename messaging.read --rolename messaging.write 2>/dev/null \
    && echo "admin-panel-bff: messaging-api client roles granted." \
    || echo "admin-panel-bff: messaging-api client role grant skipped (already present or roles absent)."

  $KCADM add-roles -r ${REALM} --uid "$AP_BFF_SA_UID" --cclientid notification-api \
    --rolename notification.admin --rolename notification.read --rolename notification.write 2>/dev/null \
    && echo "admin-panel-bff: notification-api client roles granted." \
    || echo "admin-panel-bff: notification-api client role grant skipped (already present or roles absent)."

  $KCADM add-roles -r ${REALM} --uid "$AP_BFF_SA_UID" --cclientid payment-api \
    --rolename payment.admin --rolename payment.read --rolename payment.write 2>/dev/null \
    && echo "admin-panel-bff: payment-api client roles granted." \
    || echo "admin-panel-bff: payment-api client role grant skipped (already present or roles absent)."

  $KCADM add-roles -r ${REALM} --uid "$AP_BFF_SA_UID" --cclientid profile-api \
    --rolename profile.admin --rolename profile.read --rolename profile.write 2>/dev/null \
    && echo "admin-panel-bff: profile-api client roles granted." \
    || echo "admin-panel-bff: profile-api client role grant skipped (already present or roles absent)."

  $KCADM add-roles -r ${REALM} --uid "$AP_BFF_SA_UID" --cclientid reference-data-api \
    --rolename reference-data.currency.manage --rolename reference-data.location.read \
    --rolename reference-data.lookup.manage --rolename reference-data.read \
    --rolename reference-data.write 2>/dev/null \
    && echo "admin-panel-bff: reference-data-api client roles granted." \
    || echo "admin-panel-bff: reference-data-api client role grant skipped (already present or roles absent)."

  $KCADM add-roles -r ${REALM} --uid "$AP_BFF_SA_UID" --cclientid service-request-api \
    --rolename service-request.admin --rolename service-request.assignment.manage \
    --rolename service-request.completion.manage --rolename service-request.dispute.manage \
    --rolename service-request.read --rolename service-request.write 2>/dev/null \
    && echo "admin-panel-bff: service-request-api client roles granted." \
    || echo "admin-panel-bff: service-request-api client role grant skipped (already present or roles absent)."

  $KCADM add-roles -r ${REALM} --uid "$AP_BFF_SA_UID" --cclientid vessel-api \
    --rolename vessel.admin --rolename vessel.document.manage --rolename vessel.ownership.manage \
    --rolename vessel.read --rolename vessel.write 2>/dev/null \
    && echo "admin-panel-bff: vessel-api client roles granted." \
    || echo "admin-panel-bff: vessel-api client role grant skipped (already present or roles absent)."

  _sa_roles "$AP_BFF_SA_UID" "admin-panel-bff"
else
  echo "WARN: could not resolve admin-panel-bff service-account user id — role grants skipped."
fi

# ── Dev login-user self-heal (companion) ───────────────────────────────────────
# The KC 25 init image has no psql/curl/openssl and Identity Postgres only accepts SCRAM off-box,
# so this container CANNOT read the KeycloakSubjectId Identity stores. That read (+ recreating the
# dev KC users with Identity's EXACT subject via partialImport, so admin/provider/participant survive
# a keycloak-db wipe with no 1104 / no "No Keycloak user for sub") is done by the host companion
# below, which has the repo's dev psql pattern + curl. Run it right after this init (dev only):
echo "NEXT (dev only): heal the dev login users so all portals survive this wipe —"
echo "    ./infrastructure/keycloak/dev-seed-kc-users.sh"

echo "Done."
