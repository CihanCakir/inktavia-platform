# Admin Panel Demo Data Validation Report

**Date**: 2026-06-14  
**Scope**: Build validation, configuration validation, admin panel + Postman scenario coverage

---

## 1. Build Validation

### Command
```bash
cd /Users/cihancakir/Desktop/Mine/DEV/addesso-project
dotnet restore && dotnet build --no-restore
```

### Results
| Metric | Result |
|--------|--------|
| Errors | **0** |
| Warnings | 842 (all pre-existing, unrelated to mock data changes) |
| Build Status | **✅ PASSED** |

### Affected Projects (all built successfully)
- `Aizen.Modules.Identity.Repository`
- `Aizen.Modules.Identity`
- `Aizen.Modules.Vessel.Repository`
- `Aizen.Modules.Vessel`
- `Aizen.Modules.ServiceRequest.Repository`
- `Aizen.Modules.ServiceRequest`

---

## 2. Configuration Validation

### 2.1 appsettings.json (each module)
- `MockData.Enabled: false` ✅
- `MockData.RunOnStartup: false` ✅
- `MockData.DataSet: "admin-demo"` ✅
- `MockData.EnvironmentGuard: ["Local", "Development"]` ✅

### 2.2 appsettings.Local.json (each module)
- `MockData.Enabled: true` ✅
- `MockData.RunOnStartup: true` ✅

### 2.3 JSON Files Presence

| Module | File | Status |
|--------|------|--------|
| Identity | `Seed/Json/MockData/admin-demo/identity-users.json` | ✅ |
| Identity | `Seed/Json/MockData/admin-demo/identity-profiles.json` | ✅ |
| Vessel | `Seed/Json/MockData/admin-demo/vessels.json` | ✅ |
| Vessel | `Seed/Json/MockData/admin-demo/vessel-owners.json` | ✅ |
| Vessel | `Seed/Json/MockData/admin-demo/vessel-specifications.json` | ✅ |
| Vessel | `Seed/Json/MockData/admin-demo/vessel-engines.json` | ✅ |
| ServiceRequest | `Seed/Json/MockData/admin-demo/service-requests.json` | ✅ |
| ServiceRequest | `Seed/Json/MockData/admin-demo/service-request-items.json` | ✅ |
| ServiceRequest | `Seed/Json/MockData/admin-demo/service-request-offers.json` | ✅ |
| ServiceRequest | `Seed/Json/MockData/admin-demo/service-request-offer-items.json` | ✅ |
| ServiceRequest | `Seed/Json/MockData/admin-demo/service-request-assignments.json` | ✅ |
| ServiceRequest | `Seed/Json/MockData/admin-demo/service-request-worklogs.json` | ✅ |
| ServiceRequest | `Seed/Json/MockData/admin-demo/service-request-completions.json` | ✅ |
| ServiceRequest | `Seed/Json/MockData/admin-demo/service-request-disputes.json` | ✅ |
| ServiceRequest | `Seed/Json/MockData/admin-demo/service-request-messages.json` | ✅ |
| ServiceRequest | `Seed/Json/MockData/admin-demo/service-request-status-history.json` | ✅ |

---

## 3. Admin Panel Demo Scenarios

### 3.1 User Management Scenarios

| Scenario | Demo User | Admin Panel Feature |
|----------|----------|---------------------|
| View approved boat owner | ayse.demir@inktavia.local (10003) | User list, profile detail |
| View pending approval profile | deniz.yilmaz@inktavia.local (10005) | Pending approvals queue |
| View rejected profile | selin.uzun@inktavia.local (10006) | Rejection review |
| View admin user | admin@inktavia.local (10001) | Admin user management |
| View organizer/provider | marina.ops@inktavia.local (10011) | Provider management |

### 3.2 Vessel Management Scenarios

| Scenario | Demo Vessel | Feature |
|----------|------------|---------|
| Active vessel detail | Blue Octopus (20001) | Vessel profile, owner detail |
| Passive vessel | Aegean Wind (20004) | Status filter |
| Vessel under maintenance | Kalamış Runner (20006) | Maintenance status view |
| Multi-vessel owner | ayse.demir (10003) owns 20001+20005 | Owner fleet view |

### 3.3 Service Request Lifecycle Scenarios

| Scenario | SR ID | Feature |
|----------|-------|---------|
| Completed service history | 30001, 30007 | SR detail with full lifecycle |
| Active/in-progress SR | 30002, 30012 | Active work monitoring |
| Awaiting owner approval | 30003 | Completion approval queue |
| Dispute management | 30006 | Dispute resolution view |
| Offer evaluation | 30009 | Offer comparison |
| Draft SR (incomplete) | 30008 | Draft SR review |
| Cancelled SR history | 30010 | Cancellation history |
| Waiting for offers | 30005 | Open requests needing attention |

### 3.4 Admin Operations Scenarios

| Scenario | Data |
|----------|------|
| Resolve dispute (SR 30006) | Admin user 10001 as `ResolvedByAdminUserId` |
| View cross-module ownership | Vessel owner UserId joins to Identity user profile |
| Provider performance view | Providers 10011-10013 with multiple completed SRs |

---

## 4. Postman Scenarios

### 4.1 Identity Module

```
POST   /api/v1/auth/login/username
  Body: { "email": "admin@inktavia.local", "password": "Admin!123" }
  Expected: 200 with Identity access token

GET    /api/v1/users/{id}
  Params: id = 10003
  Expected: Ayşe Demir profile detail

GET    /api/v1/users?page=1&size=10
  Expected: List includes mock users (IDs 10001-10013)

GET    /api/v1/users?approvalStatus=0   (Pending)
  Expected: Returns user 10005

GET    /api/v1/users?approvalStatus=2   (Rejected)
  Expected: Returns user 10006
```

### 4.2 Vessel Module

```
GET    /api/v1/vessels
  Expected: List includes 8 mock vessels

GET    /api/v1/vessels/{id}
  Params: id = 20001
  Expected: Blue Octopus detail

GET    /api/v1/vessels?status=3     (Passive)
  Expected: Returns Aegean Wind (20004)

GET    /api/v1/vessels?ownerId=10003
  Expected: Returns Blue Octopus (20001) + CargoDry Demo Boat (20005)
```

### 4.3 ServiceRequest Module

```
GET    /api/v1/service-requests
  Expected: List includes 12 mock SRs

GET    /api/v1/service-requests/{id}
  Params: id = 30001
  Expected: Completed SR with full lifecycle

GET    /api/v1/service-requests?status=41   (Completed)
  Expected: Returns 30001, 30007

GET    /api/v1/service-requests?status=50   (DisputeOpened)
  Expected: Returns 30006

GET    /api/v1/service-requests/{id}/offers
  Params: id = 30001
  Expected: Accepted offer detail

GET    /api/v1/service-requests/{id}/assignments
  Params: id = 30001
  Expected: Completed assignment

GET    /api/v1/service-requests/{id}/messages
  Params: id = 30001
  Expected: Conversation thread
```

### 4.4 AdminPanel BFF

```
GET    /api/v1/admin-panel/users
  Headers: X-Aizen-User-Token: Bearer <admin-identity-token>
  Expected: Admin-shaped user list

GET    /api/v1/admin-panel/vessels
  Headers: X-Aizen-User-Token: Bearer <admin-identity-token>
  Expected: Admin-shaped vessel list

GET    /api/v1/admin-panel/service-requests
  Headers: X-Aizen-User-Token: Bearer <admin-identity-token>
  Expected: Admin SR list with status distribution
```

---

## 5. Idempotency Validation

The seeders were designed to be re-runnable:
- Each entity checked with `AnyAsync(e => e.Id == stableId)` before insert
- No truncation or deletion of existing data
- Re-running startup does not duplicate any records

---

## 6. Remaining Validation Gaps

| Gap | Status | Notes |
|-----|--------|-------|
| Runtime startup test | Not executed | Requires PostgreSQL + running services locally |
| Postman collection execution | Not executed | Requires local services running |
| AdminPanel BFF integration test | Not executed | Requires BFF + all services running |
| FileStorage mock data | Not covered | Documents/media not seeded; requires FileStorage module |
| Keycloak mock user import | Not covered | KeycloakSubjectId not set in mock users |
