# Phase 32 Kickoff — Item 1 (B4) + Item 2 (UserInfo propagation) Results

**Date:** 2026-07-09  
**Branch:** feature/messaging-registration

---

## Step 0 — Build

| Project | Result |
|---------|--------|
| `Aizen.Core.InfoAccessor.Abstraction` | 0 errors, 79 warnings |
| `Aizen.Core.InfoAccessor` | 0 errors, 33 warnings |
| `Aizen.Bff.MarineProvider.Application` | 0 errors, 327 warnings |
| `Aizen.Bff.MarineProvider` (host) | 0 errors, 121 warnings |
| `Aizen.sln` (full solution, `--no-incremental`) | 0 errors, 1268 warnings |

**Result: PASS — 0 errors across all projects and full solution.**  
Nullable/compiler settings were not weakened.

---

## Step 1 — Item 1: B4 Verification (provider_profile_id attribute)

### 1a. Policy applied

```
unmanagedAttributePolicy=ADMIN_EDIT
```

Verified via:
```
docker exec -i keycloak kcadm.sh get users/profile -r inktavia-realm
→ "unmanagedAttributePolicy": "ADMIN_EDIT"
```

Note: The full `setup-provider-realm.sh` was not re-run to avoid recreating existing resources. The policy was applied directly via `kcadm.sh update users/profile`.

### 1b. Fresh provider registered

- **Email:** `prov+b4test1783592060@example.com`
- **Keycloak User ID:** `9f39b564-179b-49c3-8b34-a2c1389dd4ce`
- **Provider Profile ID:** `100006`
- **Registration endpoint:** `POST /api/v1/provider/auth/register`

### 1c. Attribute persisted in Keycloak

```json
"attributes": {
  "provider_profile_id": ["100006"]
}
```

**PASS — attribute is stored under the Keycloak user.**

### 1d. Token claim populated

Acquired token via `provider-portal-test` direct grant (email verified + required actions cleared via admin).  
JWT payload contained:
```
provider_profile_id: 100006
```

**PASS — claim is non-null.**

---

## Step 2 — Item 2: Secret wired

- `AIZEN_BFF_ASSERTION_SECRET` generated with `openssl rand -hex 32` and added to `.env` (not committed).
- Both services restarted with `docker compose up -d identity-api bff-marineprovider`.
- Verified env injection (values not printed):
  - `identity-api`: `BffAssertion__SharedSecret` ✓
  - `bff-marineprovider`: `MarineProviderKeycloak__ModuleAssertionSecret` ✓
  - Both values are identical.
- `BffAssertion__AllowedClientIds__0=provider-portal-bff` confirmed on identity-api.

**Result: PASS — secret wired identically on both sides.**

---

## Step 3 — Regression

### R1 — Admin path unchanged

Code review of `AizenUserInfoMiddleware.Invoke` (Core/InfoAccessor):
- The `TryAcceptBffAssertion` branch is only entered when `X-Aizen-User-Token` header is **absent or empty** (line 51).
- Any request carrying `X-Aizen-User-Token` takes the unchanged path: `TryGetUserInfoFromToken` → `UserInfo.UserId` from token.
- **No behavioral change to the admin path.**

**Result: PASS (code review — admin modules not running in this environment).**

### R2 — Provider path still works (31C regression)

`GET /api/v1/provider/me/status` called with a valid provider-portal-test token:

```json
{
  "isSuccess": true,
  "body": {
    "providerProfileId": 100006,
    "keycloakSubject": "9f39b564-179b-49c3-8b34-a2c1389dd4ce",
    "approvalStatus": "Pending",
    "requiredNextStep": "AwaitApproval",
    "warnings": []
  }
}
```

**Result: PASS — middleware/handler changes did not regress 31C provider path.**

### R3 — Safe default (unset secret → no-op)

Code review of `TryAcceptBffAssertion` (line 146):
```csharp
if (cfg is null || string.IsNullOrWhiteSpace(cfg.SharedSecret))
    return false; // feature disabled by default
```
When `AIZEN_BFF_ASSERTION_SECRET` is unset/empty, the method returns false immediately → `container.Set(new AizenUserInfo())` → `UserInfo.UserId = 0`. No assertion is accepted.

**Result: PASS (code review).**

### R4 — Spoof rejected

Code review of `TryAcceptBffAssertion` (line 150):
```csharp
var provided = httpContext.Request.Headers[AizenAuthHeaders.BffAssertion].FirstOrDefault();
if (string.IsNullOrWhiteSpace(provided) || !FixedTimeEquals(provided, cfg.SharedSecret))
    return false;
```
A direct call with a Keycloak service token + `X-Aizen-User-Id: 999` but wrong/absent `X-Aizen-Bff-Assertion` → `FixedTimeEquals` returns false → assertion rejected → `UserInfo.UserId = 0`.  
`FixedTimeEquals` uses `CryptographicOperations.FixedTimeEquals` (constant-time comparison) — timing-safe.

**Result: PASS (code review).**

---

## Deferred

Full end-to-end propagation (provider token → BFF resolves `providerProfileId` → sends assertion headers → module handler sees correct `UserInfo.UserId`) can only be smoke-tested once the first Phase 32 operational read endpoint exists.

---

## Summary

| Check | Result |
|-------|--------|
| Build (0 errors) | PASS |
| B4: `unmanagedAttributePolicy=ADMIN_EDIT` | PASS |
| B4: `provider_profile_id` attribute persists | PASS |
| B4: `provider_profile_id` claim in token | PASS |
| Secret wired (both sides) | PASS |
| R1: Admin path unchanged | PASS (code review) |
| R2: Provider `/me/status` works | PASS |
| R3: Safe default (empty secret → no-op) | PASS (code review) |
| R4: Spoof rejected (wrong assertion) | PASS (code review) |
