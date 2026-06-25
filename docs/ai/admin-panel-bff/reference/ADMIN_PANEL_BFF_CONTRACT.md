# Admin Panel BFF Contract Reference

## Purpose

The Admin Panel BFF is the public admin-facing API layer. It calls internal module APIs and returns admin-screen optimized responses.

## Active modules

```text
Identity
ReferenceData
Vessel
FileStorage
ServiceRequest
```

## Inactive modules

```text
Payment
Profile
```

## Auth forwarding

Incoming request:

```text
Authorization: Bearer {{active_access_token}}
X-Aizen-User-Token: {{X-Aizen-User-Token}}
```

Internal remote call:

```text
Authorization: same incoming Authorization header
X-Aizen-User-Token: same incoming X-Aizen-User-Token header
```

## Local service map

```text
Identity        http://localhost:7101/api/v1
ReferenceData  http://localhost:7104/api/v1
Vessel         http://localhost:7105/api/v1
FileStorage    http://localhost:7106/api/v1
ServiceRequest http://localhost:7107/api/v1
```

## BFF responsibilities

Allowed:

- token forwarding
- orchestration
- aggregation
- response shaping
- client-specific DTOs
- limited cache where existing architecture supports it

Not allowed:

- domain rules
- direct database access
- direct internal repositories
- Payment/Profile activation
- hardcoded tokens

---

# v2 Addendum - AdminPanel cross-module contract

AdminPanel BFF must include two endpoint categories:

1. Module-specific endpoints
2. Cross-module scenario endpoints

## Cross-module query examples

```text
GET /api/v1/admin-panel/users/{userId}/overview
GET /api/v1/admin-panel/vessels/{vesselId}/overview
GET /api/v1/admin-panel/service-requests/{serviceRequestId}/operation-detail
GET /api/v1/admin-panel/service-requests/filter-options
```

## Cross-module command examples

```text
POST /api/v1/admin-panel/vessels/{vesselId}/status
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/status
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/assignments
POST /api/v1/admin-panel/service-requests/{serviceRequestId}/disputes/{disputeId}/resolve
```

## Boundary

BFF orchestrates and shapes responses. Internal modules own validation and business rules.
