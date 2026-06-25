# Vessel Demo Seed Smoke Test Report

**Date**: 2026-06-19  
**Branch**: feature/service-request-registration  
**Scope**: Smoke test commands and expected results for Vessel and ServiceRequest demo seed data

---

## 1. Prerequisites

All services must be running locally:

```bash
# Infrastructure (via docker-compose or local)
docker-compose up -d postgres redis rabbitmq mongodb keycloak

# APIs (in separate terminals or via dotnet run)
cd Modules/Vessel/src/Aizen.Modules.Vessel && dotnet run
cd Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest && dotnet run
cd Bff/src/AdminPanel/Aizen.Bff.AdminPanel && dotnet run
```

Set environment to Local (seed runs automatically on startup):
```bash
export ASPNETCORE_ENVIRONMENT=Local
```

---

## 2. Get Admin Identity Token

```bash
# Login via AdminPanel BFF
TOKEN=$(curl -s -X POST "http://localhost:<BFF_PORT>/api/v1/admin-panel/auth/login/username" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin@inktavia.local","password":"Admin!123"}' \
  | jq -r '.body.accessToken')
echo "Token: $TOKEN"
```

---

## 3. Vessel List Smoke Test

```bash
curl -s -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels?pageIndex=0&pageSize=20" \
  -H "X-Aizen-User-Token: Bearer $TOKEN" | jq '.body'
```

**Expected**:
- `body.vessels.count >= 8`
- `body.vessels.items` array with at least 8 vessels
- No `IPaginate<T>` deserialization error

---

## 4. Vessel Detail Smoke Test

First discover a seeded vessel ID from the list response:
```bash
VESSEL_ID=$(curl -s -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels?pageIndex=0&pageSize=5" \
  -H "X-Aizen-User-Token: Bearer $TOKEN" \
  | jq '.body.vessels.items[0].id')
echo "Vessel ID: $VESSEL_ID"
```

Then fetch detail:
```bash
curl -s -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels/$VESSEL_ID/detail" \
  -H "X-Aizen-User-Token: Bearer $TOKEN" | jq '.body.vessel | {id, name, engine}'
```

**Expected**:
- `body.vessel.id` is not null
- `body.vessel.engine` is not null (for vessels 20001, 20003, 20005, 20006, 20007)

---

## 5. Vessel Documents Smoke Test

```bash
curl -s -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels/$VESSEL_ID/documents" \
  -H "X-Aizen-User-Token: Bearer $TOKEN" | jq '.body.documents | length'
```

**Expected**:
- `body.documents` array with length >= 4 for vessels 20001, 20002, 20003, 20005

---

## 6. Vessel Media Smoke Test

```bash
curl -s -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels/$VESSEL_ID/media" \
  -H "X-Aizen-User-Token: Bearer $TOKEN" | jq '.body.media | length'
```

**Expected**:
- `body.media` array with length >= 3 for vessels 20001, 20002, 20003, 20005

---

## 7. ServiceRequest History by Vessel

```bash
curl -s -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/service-requests/by-vessel/$VESSEL_ID/history?take=5" \
  -H "X-Aizen-User-Token: Bearer $TOKEN" | jq '.body | length'
```

**Expected for vessels 20001 (Blue Octopus)**:
- Returns 2 service requests (30001, 30009)

---

## 8. ServiceRequest Admin List

```bash
curl -s -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/service-requests?vesselId=$VESSEL_ID&pageIndex=0&pageSize=10" \
  -H "X-Aizen-User-Token: Bearer $TOKEN" | jq '.body'
```

**Expected**:
- `body.items` array with service request records for the given vesselId

---

## 9. Auth Guard Test (403)

```bash
# Get participant/customer token
CUSTOMER_TOKEN=$(curl -s -X POST "http://localhost:<BFF_PORT>/api/v1/admin-panel/auth/login/username" \
  -H "Content-Type: application/json" \
  -d '{"username":"ayse.demir@inktavia.local","password":"Test!123"}' \
  | jq -r '.body.accessToken')

# Attempt admin endpoint — expect 403
curl -s -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels?pageIndex=0&pageSize=5" \
  -H "X-Aizen-User-Token: Bearer $CUSTOMER_TOKEN" | jq '.statusCode'
# Expected: 403
```

---

## 10. Notes

- Replace `<BFF_PORT>` with the actual AdminPanel BFF port (check `launchSettings.json` or `appsettings.Local.json`)
- The seeded first vessel may not have database ID `1` — always discover via the list endpoint
- `AccessUrl` in media and document responses will be `null` because `FileId` is null in seed data — this is expected for local demo
