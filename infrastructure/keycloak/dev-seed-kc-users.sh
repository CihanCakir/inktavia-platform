#!/usr/bin/env bash
# ==============================================================================
# DEV-ONLY Keycloak login-user self-heal  —  DO NOT COMMIT (this path is gitignored).
#
# Problem it fixes: a `keycloak-db` volume wipe + realm re-import gives the dev login
# users a *fresh* Keycloak subject (the realm JSON pins no user id), while Identity
# Postgres keeps the OLD KeycloakSubjectId — so:
#   • admin/provider provisioning throws EmailConflictWithExistingUser (1104), and
#   • the participant ticket-SPI logs "No Keycloak user for sub …".
#
# Fix (the durable one proven live): make Keycloak match Identity, not the reverse.
# For each known dev account we READ the KeycloakSubjectId Identity already stores
# (query by email) and ensure a Keycloak user with THAT EXACT id exists via the
# Admin `partialImport` API (partialImport preserves the provided id → sub matches
# Identity → no 1104, no DB rewrite). Every user: enabled, emailVerified, real
# first/last name, requiredActions cleared (a bare user triggers VERIFY_PROFILE,
# which 404s the headless ticket handoff at hop 1).
#
# WHY a host companion (and not init.sh itself): the keycloak-init container is the
# stock KC 25 image — it has NO psql/curl/openssl, and Identity Postgres only accepts
# SCRAM auth from off-box, so init.sh cannot read the stored subject. This companion
# runs on the host where the repo's dev psql pattern (`docker compose exec postgres
# psql`) and curl are available. init.sh calls it out in its final banner; run it
# right after `docker compose up keycloak-init`. Idempotent + dev-gated.
# ==============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REALM="${KC_REALM:-inktavia-realm}"
KC="${KC_URL:-http://localhost:8080}"
KC_ADMIN="${KEYCLOAK_ADMIN:-admin}"
KC_ADMIN_PW="${KEYCLOAK_ADMIN_PASSWORD:-admin}"
PG_USER="${POSTGRES_USER:-aizen}"
PG_DB="${POSTGRES_DB:-inktavia_store}"
# provider-portal-bff confidential-client secret (keystone). From env; defaults to the dev value the compose
# files default to so every consumer (setup script, marine-provider-bff, identity-api) resolves identically.
PROVIDER_BFF_SECRET="${KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET:-local-dev-only-change-me}"
PROVIDER_WEB_BASE="${PROVIDER_WEB_BASE:-http://localhost:3002}"

# Dev gate — refuse to run outside Development/Local (never seed real creds in staging/prod).
ENVN="${ASPNETCORE_ENVIRONMENT:-Development}"
case "$ENVN" in
  Development|Local|local|development) ;;
  *) echo "dev-seed-kc-users: refusing to run in ASPNETCORE_ENVIRONMENT=$ENVN (dev only)"; exit 0 ;;
esac

echo "── Dev Keycloak user self-heal (realm=$REALM) ─────────────────────────────"

# psql helper — the repo's documented dev pattern (host → postgres container).
psqlq() { docker compose exec -T postgres psql -U "$PG_USER" -d "$PG_DB" -tAc "$1" 2>/dev/null | tr -d '\r'; }

# Allow unmanaged user attributes so the *_profile_id attributes we set actually persist. With the
# default (declaration-only) user profile, Keycloak SILENTLY DROPS any attribute not declared, so
# participant_profile_id / provider_profile_id would vanish. ADMIN_EDIT lets the Admin API write them.
# Mirrors infrastructure/keycloak/provider-realm/setup-provider-realm.sh. Idempotent.
ensure_unmanaged_attrs() {
  local tok; tok=$(kc_token)
  local cur
  cur=$(curl -s "$KC/admin/realms/$REALM/users/profile" -H "Authorization: Bearer $tok" \
    | python3 -c 'import sys,json;print(json.load(sys.stdin).get("unmanagedAttributePolicy") or "")')
  if [ "$cur" != "ADMIN_EDIT" ]; then
    local prof; prof=$(curl -s "$KC/admin/realms/$REALM/users/profile" -H "Authorization: Bearer $tok")
    printf '%s' "$prof" | python3 -c 'import sys,json;d=json.load(sys.stdin);d["unmanagedAttributePolicy"]="ADMIN_EDIT";print(json.dumps(d))' > /tmp/kc_userprofile.json
    curl -s -X PUT "$KC/admin/realms/$REALM/users/profile" -H "Authorization: Bearer $tok" \
      -H 'Content-Type: application/json' --data @/tmp/kc_userprofile.json >/dev/null || true
    rm -f /tmp/kc_userprofile.json
    echo "  realm user-profile: unmanagedAttributePolicy → ADMIN_EDIT (was ${cur:-<unset>})"
  else
    echo "  realm user-profile: unmanagedAttributePolicy already ADMIN_EDIT"
  fi
}

# Fresh admin token each call (cheap; avoids expiry across a slow psql round-trip).
kc_token() {
  curl -s -X POST "$KC/realms/master/protocol/openid-connect/token" \
    -H 'Content-Type: application/x-www-form-urlencoded' \
    -d "grant_type=password&client_id=admin-cli&username=$KC_ADMIN&password=$KC_ADMIN_PW" \
    | python3 -c 'import sys,json;print(json.load(sys.stdin).get("access_token",""))'
}

# Ensure a realm role exists (create if missing). $1=role name.
ensure_role() {
  local tok="$1" role="$2" code
  code=$(curl -s -o /dev/null -w '%{http_code}' "$KC/admin/realms/$REALM/roles/$role" -H "Authorization: Bearer $tok")
  if [ "$code" = "404" ]; then
    curl -s -X POST "$KC/admin/realms/$REALM/roles" -H "Authorization: Bearer $tok" \
      -H 'Content-Type: application/json' -d "{\"name\":\"$role\"}" >/dev/null || true
    echo "    created missing realm role '$role'"
  fi
}

# Assign realm roles to a user id (idempotent — Keycloak no-ops already-assigned). $3=space-separated roles.
assign_roles() {
  local tok="$1" uid="$2" roles="$3" reps="[" first=1 r rep
  for r in $roles; do
    ensure_role "$tok" "$r"
    rep=$(curl -s "$KC/admin/realms/$REALM/roles/$r" -H "Authorization: Bearer $tok")
    [ -z "$rep" ] && continue
    [ $first -eq 1 ] && first=0 || reps="$reps,"
    reps="$reps$rep"
  done
  reps="$reps]"
  [ "$reps" = "[]" ] && return 0
  curl -s -X POST "$KC/admin/realms/$REALM/users/$uid/role-mappings/realm" \
    -H "Authorization: Bearer $tok" -H 'Content-Type: application/json' --data "$reps" >/dev/null || true
}

# heal_account <email> <first> <last> <roleContext|-> <attrKey|-> <roles...>
#   roleContext: Identity UserProfiles.RoleContext to resolve the *_profile_id attribute (1=Participant,3=Organizer/Provider,4=Admin); '-' = none
#   attrKey:     Keycloak attribute name to carry that profile id ('-' = none)
heal_account() {
  local email="$1" first="$2" last="$3" rolectx="$4" attrkey="$5"; shift 5
  local roles="$*"

  local sub
  sub=$(psqlq "SELECT \"KeycloakSubjectId\" FROM \"Users\" WHERE lower(\"Email\")=lower('$email') AND \"IsDeleted\"=false LIMIT 1;")
  if [ -z "$sub" ]; then
    echo "  [$email] no Identity user / no stored subject — skipping"
    return 0
  fi

  # Resolve the profile-id attribute value from Identity (so the token carries it), if requested.
  local attrs_json="{}"
  if [ "$attrkey" != "-" ] && [ "$rolectx" != "-" ]; then
    local pid
    pid=$(psqlq "SELECT p.\"Id\" FROM \"UserProfiles\" p JOIN \"Users\" u ON u.\"Id\"=p.\"UserId\" WHERE lower(u.\"Email\")=lower('$email') AND p.\"RoleContext\"=$rolectx ORDER BY p.\"Id\" LIMIT 1;")
    [ -n "$pid" ] && attrs_json="{\"$attrkey\":[\"$pid\"]}"
  fi

  local tok; tok=$(kc_token)
  if [ -z "$tok" ]; then echo "  [$email] could not get admin token"; return 1; fi

  # If a KC user for this email exists with a DIFFERENT id (fresh realm-import subject), delete it
  # so we can recreate it with Identity's stored subject.
  local existing_id
  existing_id=$(curl -s "$KC/admin/realms/$REALM/users?email=$email&exact=true" -H "Authorization: Bearer $tok" \
    | python3 -c 'import sys,json;a=json.load(sys.stdin);print(a[0]["id"] if a else "")')
  if [ -n "$existing_id" ] && [ "$existing_id" != "$sub" ]; then
    curl -s -X DELETE "$KC/admin/realms/$REALM/users/$existing_id" -H "Authorization: Bearer $tok" >/dev/null || true
    echo "  [$email] removed mismatched KC user (${existing_id:0:8}… ≠ stored ${sub:0:8}…)"
    existing_id=""
  fi

  # Create the KC user with Identity's EXACT subject (partialImport preserves the id).
  if [ -z "$existing_id" ]; then
    cat >/tmp/kc_pi_${email//[^a-zA-Z0-9]/_}.json <<JSON
{"ifResourceExists":"SKIP","users":[{"id":"$sub","username":"$email","email":"$email","emailVerified":true,"enabled":true,"firstName":"$first","lastName":"$last","attributes":$attrs_json,"realmRoles":[]}]}
JSON
    curl -s -X POST "$KC/admin/realms/$REALM/partialImport" -H "Authorization: Bearer $tok" \
      -H 'Content-Type: application/json' --data @/tmp/kc_pi_${email//[^a-zA-Z0-9]/_}.json >/dev/null || true
    rm -f /tmp/kc_pi_${email//[^a-zA-Z0-9]/_}.json
    echo "  [$email] partialImport → KC user id=${sub}"
  else
    echo "  [$email] KC user already matches stored subject (${sub:0:8}…)"
  fi

  # Ensure enabled/verified/names/attrs + CLEAR requiredActions (prevents VERIFY_PROFILE 404 at handoff hop 1).
  # MUST include username+email: KC25 PUT /users/{id} REPLACES the rep, so omitting email NULLS it — and with
  # the provider realm's verifyEmail=true a null email injects VERIFY_EMAIL, rendering a page (authorize 200)
  # instead of the code redirect. Setting them keeps email intact + emailVerified true.
  tok=$(kc_token)
  curl -s -X PUT "$KC/admin/realms/$REALM/users/$sub" -H "Authorization: Bearer $tok" \
    -H 'Content-Type: application/json' \
    -d "{\"username\":\"$email\",\"email\":\"$email\",\"enabled\":true,\"emailVerified\":true,\"firstName\":\"$first\",\"lastName\":\"$last\",\"requiredActions\":[],\"attributes\":$attrs_json}" >/dev/null || true

  # Ensure realm roles.
  assign_roles "$tok" "$sub" "$roles"
  echo "  [$email] ✓ healed  (sub=$sub, attrs=$attrs_json, roles: $roles)"
}

# Bind a client's browser flow to an auth flow by alias (KC25: kcadm can't write the nested
# authenticationFlowBindingOverrides map, so use the Admin REST API — GET rep, splice, PUT). $1=clientId $2=flowAlias.
bind_browser_flow() {
  local cname="$1" flow_alias="$2"
  local tok; tok=$(kc_token)
  local fid; fid=$(curl -s "$KC/admin/realms/$REALM/authentication/flows" -H "Authorization: Bearer $tok" \
    | python3 -c "import sys,json;print(next((f['id'] for f in json.load(sys.stdin) if f.get('alias')=='$flow_alias'),''))")
  local cuuid; cuuid=$(curl -s "$KC/admin/realms/$REALM/clients?clientId=$cname" -H "Authorization: Bearer $tok" \
    | python3 -c 'import sys,json;a=json.load(sys.stdin);print(a[0]["id"] if a else "")')
  if [ -z "$fid" ] || [ -z "$cuuid" ]; then echo "    [$cname -> $flow_alias] flow/client missing — binding skipped"; return 0; fi
  curl -s "$KC/admin/realms/$REALM/clients/$cuuid" -H "Authorization: Bearer $tok" \
    | python3 -c "import sys,json;d=json.load(sys.stdin);d.setdefault('authenticationFlowBindingOverrides',{})['browser']='$fid';print(json.dumps(d))" > /tmp/kc_bindflow.json
  curl -s -o /dev/null -X PUT "$KC/admin/realms/$REALM/clients/$cuuid" -H "Authorization: Bearer $tok" \
    -H 'Content-Type: application/json' --data @/tmp/kc_bindflow.json
  rm -f /tmp/kc_bindflow.json
  local after; after=$(curl -s "$KC/admin/realms/$REALM/clients/$cuuid" -H "Authorization: Bearer $tok" \
    | python3 -c 'import sys,json;print(json.load(sys.stdin).get("authenticationFlowBindingOverrides",{}).get("browser",""))')
  [ "$after" = "$fid" ] && echo "    [$cname -> $flow_alias] browser-flow bound ✓" || echo "    WARN [$cname] flow binding did not take"
}

# Provision the provider-portal realm objects so provider login survives a keycloak-db wipe with the same
# "no manual commands" guarantee as admin/participant. Runs the committed setup-provider-realm.sh (kcadm)
# INSIDE the KC container — the keycloak-init image can't reach it (only init.sh is mounted), but this host
# companion has both the script file and docker. Ordered BEFORE the provider user heal + flow binding.
# The secret comes from env, never hardcoded. Idempotent (setup is upsert; binding is a no-op if already set).
provision_provider_realm() {
  local setup="$SCRIPT_DIR/provider-realm/setup-provider-realm.sh"
  if [ ! -f "$setup" ]; then echo "  provider setup script not found ($setup) — skipping provider provisioning"; return 0; fi
  echo "  provisioning provider realm via setup-provider-realm.sh (secret from env)…"
  docker compose exec -T \
    -e KEYCLOAK_URL=http://localhost:8080 \
    -e KEYCLOAK_ADMIN="$KC_ADMIN" -e KEYCLOAK_ADMIN_PASSWORD="$KC_ADMIN_PW" \
    -e KEYCLOAK_PROVIDER_BFF_CLIENT_SECRET="$PROVIDER_BFF_SECRET" \
    -e PROVIDER_WEB_BASE="$PROVIDER_WEB_BASE" \
    keycloak bash -c 'export PATH="$PATH:/opt/keycloak/bin"; bash /dev/stdin' < "$setup" 2>&1 \
    | grep -viE 'level=warning' | grep -iE 'Creating|Upserting|id=|already exists|Granting|Done|WARN|error' | sed 's/^/    /' || echo "    (provider setup returned non-zero — continuing; check output above)"
  # The setup grants identity-api CLIENT roles (which don't exist); the provider OTP endpoints are
  # [Authorize(IdentityWrite)] → the SA needs the REALM roles identity_read/write (granted below via
  # heal_service_account). Bind the Provider OTP flow now that provider-portal exists (init.sh bound it
  # too early, when the client was still absent).
  bind_browser_flow "provider-portal" "Provider OTP Login browser"
}

# Heal a BFF service account's realm roles. init.sh grants these too, but its grant resolves the SA
# user via `clients/{id}/service-account-user`, which (with the realm-JSON-baked marine-mobile-bff)
# returns a PHANTOM id ≠ the real SA user — so the grant silently no-ops and BFF→Identity calls 403.
# We resolve the SA user reliably from the client-credentials token's `sub` and grant there, then
# restart the BFF if a role was actually missing (it caches a role-less service token until expiry).
# $1=clientId $2=clientSecret $3=containerName $4..=realm roles.
heal_service_account() {
  local cid="$1" secret="$2" container="$3"; shift 3
  local roles="$*"
  local tokresp; tokresp=$(curl -s -X POST "$KC/realms/$REALM/protocol/openid-connect/token" \
    -H 'Content-Type: application/x-www-form-urlencoded' \
    -d "grant_type=client_credentials&client_id=$cid&client_secret=$secret")
  local at; at=$(printf '%s' "$tokresp" | python3 -c 'import sys,json;print(json.load(sys.stdin).get("access_token",""))' 2>/dev/null)
  if [ -z "$at" ]; then echo "  [$cid SA] no client-credentials token (client absent?) — skip"; return 0; fi
  local sub have_missing=0 r
  sub=$(python3 -c "import base64,json;print(json.loads(base64.urlsafe_b64decode('$at'.split('.')[1]+'==')).get('sub',''))")
  local have; have=$(python3 -c "import base64,json;print(' '.join(json.loads(base64.urlsafe_b64decode('$at'.split('.')[1]+'==')).get('realm_access',{}).get('roles',[])))")
  for r in $roles; do case " $have " in *" $r "*) ;; *) have_missing=1;; esac; done
  if [ "$have_missing" -eq 0 ]; then echo "  [$cid SA] roles already present ($roles)"; return 0; fi
  local tok; tok=$(kc_token)
  for r in $roles; do
    ensure_role "$tok" "$r"
    local rep; rep=$(curl -s "$KC/admin/realms/$REALM/roles/$r" -H "Authorization: Bearer $tok")
    [ -n "$rep" ] && curl -s -X POST "$KC/admin/realms/$REALM/users/$sub/role-mappings/realm" \
      -H "Authorization: Bearer $tok" -H 'Content-Type: application/json' --data "[$rep]" >/dev/null || true
  done
  echo "  [$cid SA] granted missing realm roles to real SA user ${sub:0:8}… ($roles)"
  if [ -n "$container" ]; then
    docker compose restart "$container" >/dev/null 2>&1 && echo "  [$cid SA] restarted $container (drop cached role-less service token)" || true
  fi
}

# ── Known dev accounts ────────────────────────────────────────────────────────
ensure_unmanaged_attrs

# Provider portal parity: create provider-portal / provider-portal-bff clients + roles + mappers + OTP-flow
# binding (init.sh only self-heals admin/participant clients; provider clients come from the setup script).
# MUST run before the provider user heal (needs the provider_user role) and before the provider SA heal.
provision_provider_realm

# BFF service accounts — OTP send/verify are BFF→Identity [Authorize(IdentityWrite)] calls; the SA needs the
# identity_read/write REALM roles (the mobile grant works around an init.sh phantom-SA-id bug; the provider
# grant supplies the roles the setup script couldn't — it only grants nonexistent identity-api client roles).
heal_service_account "marine-mobile-bff" "${KEYCLOAK_MARINE_MOBILE_BFF_CLIENT_SECRET:-local-dev-only-change-me}" \
  "bff-marine-mobile" identity_read identity_write
heal_service_account "provider-portal-bff" "$PROVIDER_BFF_SECRET" \
  "bff-marineprovider" identity_read identity_write

# ADMIN — Admin + admin_user + the module read/write set the admin BFF token carries.
heal_account "admin.user@inktavia.com" "Admin" "User" 4 "-" \
  Admin admin_user identity_read identity_write profile_read profile_write \
  payment_read payment_write vessel_read vessel_write file_storage_read file_storage_write \
  service_request_read service_request_write reference_data_read reference_data_write

# PROVIDER — provider_user (+ profile_id attribute so the token scopes to the provider).
heal_account "provider2@inktavia.com" "Provider" "Two" 3 "provider_profile_id" \
  provider_user profile_read profile_write vessel_read file_storage_read file_storage_write \
  service_request_read service_request_write reference_data_read

# PARTICIPANT (mobile owner) — mobile_user + participant_profile_id attribute.
heal_account "qa.owner.aug5@inktavia.com" "QA" "Owner" 1 "participant_profile_id" \
  mobile_user profile_read profile_write vessel_read file_storage_read file_storage_write \
  service_request_read service_request_write reference_data_read

echo "── Dev Keycloak user self-heal complete ───────────────────────────────────"
