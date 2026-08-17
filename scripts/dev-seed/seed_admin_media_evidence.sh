#!/bin/bash
# ─────────────────────────────────────────────────────────────────────────────
# DEV-ONLY seed — DO NOT COMMIT. Uploads REAL objectful files to MinIO and wires
# them to real domain rows so every admin image/evidence surface renders live
# (vessel media/hero/documents + SR request-attachment / completion-evidence /
# work-log photo, plus a non-image PDF download row).
#
# Idempotent: vessel 100013 media/docs are cleared+recreated; SR 30001 dev-seed
# attachment/work-log rows are keyed by a '[DEV-SEED]' Title marker (delete+insert)
# and the completion EvidenceFileId is overwritten. Re-runnable. Touches NO prod
# seeder — everything here is a live-endpoint call or a targeted dev insert.
#
# Real flows used where an endpoint exists:
#   • Vessel: admin BFF two-step upload  (/vessels/{id}/media|documents/upload-url → PUT → register)
#   • SR objects: FileStorage upload-session (/api/v1/upload-sessions → PUT → /complete)
#   • SR domain rows: dev inserts referencing the objectful fileIds (no owner/provider endpoint path)
# ─────────────────────────────────────────────────────────────────────────────
set -uo pipefail   # NOT -e: this is a best-effort seed; critical steps are checked explicitly.

BFF=http://localhost:17001
FS=http://localhost:7106
KC=http://localhost:8080
REALM=inktavia-realm
VESSEL_ID=100013
SR_ID=30001
SR_COMPLETION_ID=80001
SR_ASSIGNMENT_ID=60001
SR_PROVIDER_UID=10011
SR_OWNER_UID=10003
HERE="$(cd "$(dirname "$0")" && pwd)"
ASSETS="${SEED_ASSETS_DIR:-$HERE/assets}"

# Self-contained: generate the real PNG/PDF assets if missing (pure-stdlib, idempotent).
[ -f "$ASSETS/vessel_cover.png" ] || python3 "$HERE/gen_assets.py" "$ASSETS"

jget() { python3 -c 'import sys,json
try: d=json.load(sys.stdin)
except Exception: print(""); sys.exit(0)
for p in "'"$1"'".split("."):
    d=d.get(p) if isinstance(d,dict) else None
    if d is None: break
print(d if d is not None else "")' 2>/dev/null; }
psql() { docker compose exec -T postgres psql -U aizen -d inktavia_store "$@"; }

echo "── minting tokens ─────────────────────────────────────────"
# admin-panel-bff SERVICE token (client_credentials) — aud includes file-storage-api (for direct FS calls)
SVC_CID=$(docker compose exec -T bff-adminpanel sh -lc 'printenv AdminPanelKeycloak__AdminPanelBffClientId' | tr -d '\r\n')
SVC_SEC=$(docker compose exec -T bff-adminpanel sh -lc 'printenv AdminPanelKeycloak__AdminClientSecret' | tr -d '\r\n')
SVC_TOKEN=$(curl -s -X POST "$KC/realms/$REALM/protocol/openid-connect/token" \
  -d grant_type=client_credentials -d client_id="$SVC_CID" -d client_secret="$SVC_SEC" | jget access_token)
[ -n "$SVC_TOKEN" ] || { echo "FATAL: no service token"; exit 1; }
echo "  service token OK (aud file-storage-api)"

# admin FE token (aud admin-panel-bff) via the OTP → Keycloak login-ticket handoff (dev OTP log must be on)
docker compose exec -T redis redis-cli -n 13 --scan --pattern 'admin:otplogin:rl:*' 2>/dev/null | \
  xargs -r -I{} docker compose exec -T redis redis-cli -n 13 DEL {} >/dev/null 2>&1 || true
LRID=$(curl -s -X POST "$BFF/api/v1/admin-panel/auth/otp-login/request" -H 'Content-Type: application/json' \
  -d '{"channel":"email","identifier":"admin.user@inktavia.com"}' | jget body.loginRequestId)
sleep 1
OTP=$(docker compose logs --since 15s identity-api 2>/dev/null | grep "DEV-ONLY" | tail -1 | grep -oE '[0-9]{6}' | tail -1 || true)
TICKET=$(curl -s -X POST "$BFF/api/v1/admin-panel/auth/otp-login/verify" -H 'Content-Type: application/json' \
  -d "{\"loginRequestId\":\"$LRID\",\"otpCode\":\"$OTP\"}" | jget body.loginTicket)
read -r VERIF CHAL < <(python3 -c 'import os,base64,hashlib;v=base64.urlsafe_b64encode(os.urandom(40)).rstrip(b"=").decode();print(v, base64.urlsafe_b64encode(hashlib.sha256(v.encode()).digest()).rstrip(b"=").decode())')
CODE=$(curl -s -c /tmp/kcjar -o /dev/null -D - \
  "$KC/realms/$REALM/protocol/openid-connect/auth?client_id=admin-panel&redirect_uri=http://localhost:3000/auth/callback&response_type=code&response_mode=query&scope=openid&state=s&nonce=n&code_challenge=$CHAL&code_challenge_method=S256&login_ticket=$TICKET" \
  | grep -i '^location:' | tr -d '\r' | sed -n 's/.*[?&]code=\([^&]*\).*/\1/p')
FE_TOKEN=$(curl -s -X POST "$KC/realms/$REALM/protocol/openid-connect/token" \
  -d grant_type=authorization_code -d client_id=admin-panel -d code="$CODE" \
  -d redirect_uri=http://localhost:3000/auth/callback -d code_verifier="$VERIF" | jget access_token)
[ -n "$FE_TOKEN" ] || { echo "FATAL: no admin FE token (OTP code=$OTP)"; exit 1; }
echo "  admin FE token OK (aud admin-panel-bff)"

# ── Part 1: Vessel 100013 media + document via the admin BFF two-step upload ──
echo "── vessel $VESSEL_ID: clearing old placeholder media/docs (dev) ──────────"
psql -q -c "DELETE FROM vessel.vessel_media     WHERE \"VesselId\"=$VESSEL_ID;" >/dev/null
psql -q -c "DELETE FROM vessel.vessel_documents WHERE \"VesselId\"=$VESSEL_ID;" >/dev/null
docker compose exec -T redis redis-cli -n 11 FLUSHDB >/dev/null 2>&1 || true   # vessel read cache

# $1 kind(media|documents) $2 file $3 contentType -> echoes "fileId uploadSessionCode"
bff_upload() {
  local kind="$1" file="$2" ct="$3" size resp fid url code
  size=$(wc -c < "$file" | tr -d ' ')
  resp=$(curl -s -X POST "$BFF/api/v1/admin-panel/vessels/$VESSEL_ID/$kind/upload-url" \
    -H "Authorization: Bearer $FE_TOKEN" -H 'Content-Type: application/json' \
    -d "{\"fileName\":\"$(basename "$file")\",\"contentType\":\"$ct\",\"fileSizeBytes\":$size}")
  fid=$(printf '%s' "$resp" | jget body.fileId); url=$(printf '%s' "$resp" | jget body.uploadUrl); code=$(printf '%s' "$resp" | jget body.uploadSessionCode)
  [ -n "$url" ] || { echo "  ERR upload-url($kind): $resp" >&2; return 1; }
  local put; put=$(curl -s -o /dev/null -w '%{http_code}' -X PUT "$url" -H "Content-Type: $ct" --data-binary @"$file")
  [ "$put" = "200" ] || { echo "  ERR MinIO PUT($kind) HTTP $put" >&2; return 1; }
  printf '%s %s' "$fid" "$code"
}

echo "── vessel $VESSEL_ID: uploading cover + second image + registration PDF ──"
read -r M1_FID M1_CODE < <(bff_upload media "$ASSETS/vessel_cover.png" image/png)
curl -s -o /dev/null -X POST "$BFF/api/v1/admin-panel/vessels/$VESSEL_ID/media" -H "Authorization: Bearer $FE_TOKEN" \
  -H 'Content-Type: application/json' -d "{\"fileId\":\"$M1_FID\",\"uploadSessionCode\":\"$M1_CODE\",\"mediaType\":1,\"isCover\":true,\"sortOrder\":0}"
echo "  media(cover) fileId=$M1_FID"
read -r M2_FID M2_CODE < <(bff_upload media "$ASSETS/vessel_second.png" image/png)
curl -s -o /dev/null -X POST "$BFF/api/v1/admin-panel/vessels/$VESSEL_ID/media" -H "Authorization: Bearer $FE_TOKEN" \
  -H 'Content-Type: application/json' -d "{\"fileId\":\"$M2_FID\",\"uploadSessionCode\":\"$M2_CODE\",\"mediaType\":1,\"isCover\":false,\"sortOrder\":1}"
echo "  media(second) fileId=$M2_FID"
read -r D1_FID D1_CODE < <(bff_upload documents "$ASSETS/vessel_doc.pdf" application/pdf)
curl -s -o /dev/null -X POST "$BFF/api/v1/admin-panel/vessels/$VESSEL_ID/documents" -H "Authorization: Bearer $FE_TOKEN" \
  -H 'Content-Type: application/json' -d "{\"fileId\":\"$D1_FID\",\"uploadSessionCode\":\"$D1_CODE\",\"documentTypeCode\":\"REGISTRATION\",\"documentName\":\"[DEV-SEED] Registration Certificate\"}"
echo "  document(REGISTRATION) fileId=$D1_FID"

# ── Part 2: SR 30001 evidence — objects via FileStorage upload-session, rows via dev insert ──
# $1 file $2 contentType $3 category(1=Image,2=Document) -> echoes fileId
fs_upload() {
  local file="$1" ct="$2" cat="$3" size resp fid url code put
  size=$(wc -c < "$file" | tr -d ' ')
  resp=$(curl -s -X POST "$FS/api/v1/upload-sessions" -H "Authorization: Bearer $SVC_TOKEN" -H 'Content-Type: application/json' \
    -d "{\"originalFileName\":\"$(basename "$file")\",\"contentType\":\"$ct\",\"sizeInBytes\":$size,\"category\":$cat,\"visibility\":2,\"ownerModule\":\"ServiceRequest\",\"serverSideUpload\":false}")
  fid=$(printf '%s' "$resp" | jget body.fileId); url=$(printf '%s' "$resp" | jget body.uploadUrl); code=$(printf '%s' "$resp" | jget body.uploadSessionCode)
  [ -n "$url" ] || { echo "  ERR FS session: $resp" >&2; return 1; }
  put=$(curl -s -o /dev/null -w '%{http_code}' -X PUT "$url" -H "Content-Type: $ct" --data-binary @"$file")
  [ "$put" = "200" ] || { echo "  ERR FS PUT HTTP $put" >&2; return 1; }
  curl -s -o /dev/null -X POST "$FS/api/v1/upload-sessions/$code/complete" -H "Authorization: Bearer $SVC_TOKEN" \
    -H 'Content-Type: application/json' -d '{}'
  printf '%s' "$fid"
}

echo "── SR $SR_ID: uploading request img/pdf, completion img, worklog img ─────"
SR_REQ_IMG=$(fs_upload "$ASSETS/sr_request.png"    image/png       1); echo "  request image      fileId=$SR_REQ_IMG"
SR_REQ_PDF=$(fs_upload "$ASSETS/sr_request.pdf"    application/pdf 2); echo "  request PDF        fileId=$SR_REQ_PDF"
SR_CMP_IMG=$(fs_upload "$ASSETS/sr_completion.png" image/png       1); echo "  completion image   fileId=$SR_CMP_IMG"
SR_WL_IMG=$(fs_upload  "$ASSETS/sr_worklog.png"    image/png       1); echo "  worklog image      fileId=$SR_WL_IMG"

echo "── SR $SR_ID: wiring domain rows (idempotent [DEV-SEED]) ─────────────────"
psql -q <<SQL >/dev/null
DELETE FROM servicerequest.service_request_attachments WHERE "ServiceRequestId"=$SR_ID AND "Title" LIKE '[DEV-SEED]%';
DELETE FROM servicerequest.service_request_work_logs    WHERE "ServiceRequestId"=$SR_ID AND "Title" LIKE '[DEV-SEED]%';

INSERT INTO servicerequest.service_request_attachments
  ("ServiceRequestId","FileId","AttachmentType","Title","UploaderUserId","UploaderActorType","IsDeleted","IsActive","CreateDate")
VALUES
  ($SR_ID,'$SR_REQ_IMG',1,'[DEV-SEED] Request photo', $SR_OWNER_UID,1,false,true,now()),
  ($SR_ID,'$SR_REQ_PDF',3,'[DEV-SEED] Request document (PDF)', $SR_OWNER_UID,1,false,true,now());

UPDATE servicerequest.service_request_completions
  SET "EvidenceFileId"='$SR_CMP_IMG', "ModifyDate"=now()
  WHERE "Id"=$SR_COMPLETION_ID;

INSERT INTO servicerequest.service_request_work_logs
  ("ServiceRequestId","ServiceRequestAssignmentId","ProviderUserId","LogType","Title","Description","AttachmentFileId","LoggedAt","IsDeleted","IsActive","CreateDate")
VALUES
  ($SR_ID,$SR_ASSIGNMENT_ID,$SR_PROVIDER_UID,4,'[DEV-SEED] Work photo','Inspection completed — evidence photo attached.','$SR_WL_IMG',now(),false,true,now());
SQL

echo ""
echo "════════════════════════════════════════════════════════════════════════"
echo "SEED DONE. Wired ids:"
echo "  Vessel $VESSEL_ID  media(cover)=$M1_FID  media=$M2_FID  doc(REGISTRATION)=$D1_FID"
echo "  SR $SR_ID  reqImg=$SR_REQ_IMG  reqPdf=$SR_REQ_PDF  completion($SR_COMPLETION_ID).evidence=$SR_CMP_IMG  worklog=$SR_WL_IMG"
echo "════════════════════════════════════════════════════════════════════════"
