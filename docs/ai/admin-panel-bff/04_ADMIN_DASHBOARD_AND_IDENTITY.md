# 04 - Admin Dashboard and Identity BFF Endpoints

## Goal

Create Admin Panel BFF endpoints and Application handlers for dashboard and Identity-related admin operations using active internal endpoints.

## Dashboard

Create a dashboard endpoint only by aggregating available active module APIs.

Candidate endpoint:

```text
GET /api/v1/admin-panel/dashboard/summary
```

Candidate response sections:

```text
serviceRequestSummary
pendingCompletionCount
disputeCount
activeVesselCount
fileReviewCount
recentActivity
systemStatus
```

If some sections cannot be supported by existing module endpoints, return them as `null`, `0`, or omit them according to existing API style, and document the limitation.

## Internal calls for dashboard

Potential internal modules:

- ServiceRequest: counts/list/pending/dispute endpoints
- Vessel: count/list endpoints
- FileStorage: file review/list endpoints
- Identity: admin user/profile counts if available
- ReferenceData: lookup readiness/health only if needed

## Identity admin operations

Use discovered Identity admin endpoints if they exist.

Candidate Admin BFF endpoints:

```text
GET /api/v1/admin-panel/identity/users
GET /api/v1/admin-panel/identity/users/{userId}
GET /api/v1/admin-panel/identity/profiles
GET /api/v1/admin-panel/identity/profiles/{profileId}
POST /api/v1/admin-panel/identity/organizers/{userId}/profiles/{profileId}/approve
POST /api/v1/admin-panel/identity/organizers/{userId}/profiles/{profileId}/reject
POST /api/v1/admin-panel/identity/venues/{userId}/profiles/{profileId}/approve
POST /api/v1/admin-panel/identity/venues/{userId}/profiles/{profileId}/reject
```

Only generate endpoints if corresponding internal endpoints exist and are active.

## Requirements

- Use `AizenRemoteCall`.
- Forward auth headers.
- Use Identity Abstraction contracts where possible.
- Create BFF-specific response DTOs only when needed.
- Add DocumentationInfo if the repository convention requires it.
- Add Postman requests for every generated endpoint.

## Output

Update docs:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/docs/admin-panel-bff-dashboard-identity.md
```
