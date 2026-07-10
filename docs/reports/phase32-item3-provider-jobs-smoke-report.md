# Phase 32 — Item 3: Provider Assigned Jobs Smoke Report

**Date:** 2026-07-09  
**Branch:** feature/messaging-registration  
**Stack:** Docker Compose (local)

---

## 1. Build Result

```
dotnet build Aizen.sln -c Debug
```

**Result: 0 errors, 16 warnings** (all pre-existing nullability warnings, none new).

---

## 2. Runtime Secret Wiring

Secret set in `.env`:
```
AIZEN_BFF_ASSERTION_SECRET=1e59347d00092351e5842e24514bd1b3d01f2869b7c312df7066ef402be266c5
```

Verified via `docker compose config`:
- `bff-marineprovider`: `MarineProviderKeycloak__ModuleAssertionSecret` ✓
- `service-request-api`: `BffAssertion__SharedSecret` ✓ and `BffAssertion__AllowedClientIds__0=provider-portal-bff` ✓

Same 64-char hex value on both sides — assertion is **enabled**.

---

## 3. Keycloak Audience Fix

The `provider-portal-bff` CC token originally only had `aud: ['identity-api', ...]` — missing `service-request-api`.

**Action taken:** Added a `hardcoded-audience` protocol mapper (`aud-service-request-api`) to the `provider-portal-bff` Keycloak client via `kcadm.sh create clients/<id>/protocol-mappers/models -f /tmp/aud-mapper.json`.

CC token now carries `aud: ['identity-api', 'service-request-api', 'realm-management', 'account']`. Module JWT validation passes.

---

## 4. Bug Fix: ProviderActive Policy 403

**Root cause:** `ProviderProfileAuthorizationHandler` called `GetOrganizerProfileById(profileId)` which maps to `GET /api/v1/identity/organizers/profiles/{profileId}` — an endpoint decorated `[Authorize(Roles = RoleNames.Admin)]`. The BFF service account only holds `IdentityRead/IdentityWrite` roles, not `Admin`, causing 403 on every authorization check.

**Fix (minimal wiring):** Changed the handler to call `GetOrganizerProfileByKeycloakSubject(keycloakSubject)` instead. This endpoint is in `ProviderLinkController` with `[Authorize(Policy = "IdentityRead")]`, which the BFF service account satisfies. Same `OrganizerProfileDetailDto` shape with identical `ApprovalStatus` and `Status` fields.

**File changed:** `Bff/src/MarineProvider/Aizen.Bff.MarineProvider.Application/Common/Authorization/ProviderAuthorization.cs`

**Note:** `ProviderProfileResolver` also calls `GetOrganizerProfileById` first but already had a `by-subject` fallback (with swallowed exception). That path works end-to-end through the fallback. No change needed there.

---

## 5. Smoke Test Results

**Test data:**
- Provider A: profileId=100006, `prov+b4test1783592060@example.com`, ApprovalStatus=Approved, Status=Active
- Provider B: profileId=100007, `prov+isolation1783596077@example.com`, ApprovalStatus=Approved, Status=Active, no assignments
- Pending Provider: profileId=100008, `prov+pending1783596129@example.com`, ApprovalStatus=Pending, Status=Pending
- Assignment seeded: `service_request_assignments` row id=4, ProviderProfileId=100006, ServiceRequestId=30009, ServiceRequestOfferId=50007, Status=Accepted

| Test | Description | Request | Response | Result |
|------|-------------|---------|----------|--------|
| 5a | Happy path (BFF end-to-end) | `GET /api/v1/provider/jobs` as Provider A | HTTP 200, `hasProfileLink=true`, `items=[{assignmentId:4}]`, `warnings=[]` | **PASS** |
| 5b | Isolation (Provider B) | `GET /api/v1/provider/jobs` as Provider B | HTTP 200, `hasProfileLink=true`, `items=[]`, `totalReturned=0` | **PASS** |
| 5c | No assertion (direct module) | `GET /api/v1/service-requests/provider/jobs` with service token only | HTTP 200, `items=[]` (no profile in context) | **PASS** |
| 5d | Tampered assertion | Same as 5c + `X-Aizen-Bff-Assertion: TAMPERED_INVALID_HMAC_VALUE_12345`, `X-Aizen-Provider-Profile-Id: 100006` | HTTP 200, `items=[]` (assertion rejected, profile not set) | **PASS** |
| 5e(i) | Policy gate: pending provider | `GET /api/v1/provider/jobs` as pending provider | HTTP 403 | **PASS** |
| 5e(ii) | Policy gate: no linked profile | `GET /api/v1/provider/jobs` as plain Keycloak user | HTTP 403 | **PASS** (see note) |

### 5a — Full Response Snippet
```json
{
  "header": { "isSuccess": true, "errorCode": 0 },
  "body": {
    "hasProfileLink": true,
    "pageIndex": 0,
    "pageSize": 50,
    "totalReturned": 1,
    "message": "OK",
    "items": [
      {
        "assignmentId": 4,
        "serviceRequestId": 30009,
        "serviceRequestOfferId": 50007,
        "status": "Accepted"
      }
    ],
    "warnings": []
  }
}
```

### 5b — Full Response Snippet (Provider B)
```json
{
  "header": { "isSuccess": true, "errorCode": 0 },
  "body": {
    "hasProfileLink": true,
    "pageIndex": 0,
    "pageSize": 50,
    "totalReturned": 0,
    "message": "OK",
    "items": [],
    "warnings": []
  }
}
```

### 5c/5d — Module Direct Call Response
```json
{
  "header": { "isSuccess": true, "errorCode": 0 },
  "body": {
    "items": [],
    "pageIndex": 0,
    "pageSize": 50,
    "totalReturned": 0
  }
}
```

### 5e(ii) — Intended Behavior Note
The `ProviderActive` policy handler returns without calling `Succeed` when `ProviderProfileId` is null (no `provider_profile_id` claim). This means the endpoint is fully blocked at the authorization layer (HTTP 403) before the query handler's `hasProfileLink=false` branch is reached. This is the correct, more restrictive behavior — `ProviderActive` is designed for approved+active providers only, not for general authenticated users.

---

## 6. Follow-ups

1. ~~**`ProviderProfileResolver` minor cleanup (non-blocking):** It still calls `GetOrganizerProfileById` first (403, swallowed) then falls back to `by-subject`. This works but logs an unnecessary warning on every request. Consider switching the primary path to `by-subject` to eliminate the warning noise.~~ **Done — see Section 7.**

2. **No-profile user behavior documentation:** Confirm with the product team whether a user with no linked profile reaching `GET /api/v1/provider/jobs` should receive 403 (current, correct for ProviderActive) or be redirected to an onboarding flow via a different policy.

---

## 7. Resolver Cleanup Verification (2026-07-09)

### What changed
`ProviderProfileResolver.ResolveAsync` — removed the primary `GetOrganizerProfileById(claimId)` branch (admin-only endpoint, always 403 for the BFF service account, swallowed as warning). Resolution now goes directly to `GetOrganizerProfileByKeycloakSubject(subject)`. Behavior identical; the old code always fell through to this path anyway.

### Build
```
dotnet build Aizen.sln -c Debug → 0 errors, 16 warnings (all pre-existing)
```

### Regression (5a / 5b)

| Test | HTTP | Key fields | Result |
|------|------|-----------|--------|
| 5a — Provider A | 200 | `hasProfileLink=true`, `items=[{assignmentId:4}]`, `warnings=[]` | **PASS** |
| 5b — Provider B | 200 | `hasProfileLink=true`, `items=[]`, `totalReturned=0` | **PASS** |

### Log check
`docker compose logs bff-marineprovider | grep "claim id"` → **no output**.  
The "Resolve provider profile by claim id failed" warning no longer appears. Only pre-existing infrastructure warnings (DataProtection, HTTPS redirect) remain in the log.

3. **`provider-portal-test` client:** Used for direct-grant testing. Ensure this client is not present in production realms.
