# ServiceRequest BFF Smoke Test Report

## Build Validation

### ServiceRequest Module
```bash
dotnet build Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest/Aizen.Modules.ServiceRequest.csproj --no-incremental
```
**Result:** ✅ Build succeeded — 0 errors, warnings are pre-existing nullability warnings unrelated to this work.

### AdminPanel BFF
```bash
dotnet build Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Aizen.Bff.AdminPanel.csproj --no-incremental
```
**Result:** ✅ Build succeeded — 0 errors

## Smoke Test Instructions

### Prerequisites
1. ServiceRequest module running at `http://localhost:7107`
2. AdminPanel BFF running at `http://localhost:<BFF_PORT>`
3. Keycloak running (BFF acquires service token server-side)
4. Valid Identity admin access token from `POST /api/v1/auth/login/username`

### Test 1 — ServiceRequest List (admin)

```bash
curl -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/service-requests?pageIndex=0&pageSize=20" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>"
```

**Expected response shape:**
```json
{
  "header": { "isSuccess": true },
  "body": {
    "serviceRequests": {
      "from": 0,
      "index": 0,
      "size": 20,
      "count": N,
      "pages": P,
      "hasPrevious": false,
      "hasNext": false,
      "items": [
        {
          "id": 1,
          "requestCode": "SR-2026-0001",
          "vesselId": 1,
          "serviceType": "ANNUAL_SURVEY",
          "serviceCategoryCode": "SURVEY",
          "title": "Annual survey for vessel",
          "status": "open",
          "priority": "normal",
          "location": "Bodrum Marina",
          "notes": "Annual certificate renewal",
          "ownerUserId": 123,
          "providerProfileId": null,
          "offerCount": 0,
          "hasActiveAssignment": false,
          "requestedDate": "2026-06-21T09:00:00Z",
          "lastActivityAt": "2026-06-19T12:00:00Z",
          "createdAt": "2026-06-19T10:00:00Z"
        }
      ]
    },
    "warnings": []
  }
}
```

### Test 2 — ServiceRequest List with VesselId Filter

```bash
curl -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/service-requests?vesselId=1&pageIndex=0&pageSize=10" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>"
```

**Expected:** List filtered to vessel ID 1 only.

### Test 3 — Service History by Vessel

```bash
curl -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/service-requests/by-vessel/1/history?take=5" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>"
```

**Expected:**
```json
{
  "body": {
    "serviceHistory": [
      {
        "id": 1,
        "date": "2026-03-10T00:00:00Z",
        "serviceType": "ANNUAL_SURVEY",
        "provider": null,
        "location": "Bodrum Marina",
        "notes": "Annual certificate renewal",
        "status": "completed"
      }
    ],
    "warnings": []
  }
}
```

### Test 4 — Vessel Detail (serviceHistory populated)

```bash
curl -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels/1" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>"
```

**Expected:** `body.vessel.serviceHistory` contains items with `location`, `notes` now populated (not just `id`, `date`, `serviceType`, `status`).

### Test 5 — Auth: No Token (401)

```bash
curl -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/service-requests"
```
**Expected:** 401 Unauthorized

### Test 6 — Auth: Customer Token (403)

```bash
curl -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/service-requests" \
  -H "X-Aizen-User-Token: Bearer <customer_only_identity_token>"
```
**Expected:** 403 Forbidden

### Test 7 — Degraded Mode (SR module down)

With ServiceRequest module stopped:
```bash
curl -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/service-requests"
```
**Expected:**
```json
{
  "body": {
    "serviceRequests": null,
    "warnings": [{ "module": "ServiceRequest", "message": "..." }]
  }
}
```

### Test 8 — Vessel Detail Degraded Mode (SR module down)

```bash
curl -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels/1"
```
**Expected:** Vessel detail returns normally with `serviceHistory: []` and a warning entry, not a hard failure.

## Manual Steps to Run

| Step | Command |
|------|---------|
| Start SR module | `dotnet run --project Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest` |
| Start AdminPanel BFF | `dotnet run --project Bff/src/AdminPanel/Aizen.Bff.AdminPanel` |
| Get admin token | `POST /api/v1/auth/login/username` on Identity API |
