---
name: Admin Users BFF implementation
description: P0/P1/P2 endpoints and activity feed status for the AdminPanel BFF
type: project
---

P0/P1/P2 admin user endpoints implemented and working (list, KPI, detail, quick, vessels tab).

Activity feed (`GET /api/v1/admin-panel/admin/users/{profileId}/activity`) fully implemented with:
- Pagination + category/date filters
- Identity: profile_created event, login history events (via new `/api/v1/identity/admin/users/{userId}/login-history` endpoint)
- Vessel: vessel_registered events + vessel_status_changed events (via new `/api/v1/admin/vessels/status-history/by-owner` endpoint)
- ServiceRequest: service_request_created events

**Remaining TODOs:**
- Payment transactions: Payment module has no DB entities yet (stub) — add when module is built
- ServiceRequest status history: entity exists but no admin HTTP endpoint yet
- Email verification / profile update events: no audit log table in Identity

**Why:** Activity feed aggregates cross-module events in-memory in the BFF handler (graceful degradation per module).

**How to apply:** When Payment module gains DB entities, add a `GetAdminServiceRequestStatusHistory`-style endpoint and a new collect block in `GetAdminUserActivityBffQueryHandler.cs`.
