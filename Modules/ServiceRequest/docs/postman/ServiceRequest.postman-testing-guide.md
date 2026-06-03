# ServiceRequest Module — Postman Testing Guide

## Overview
ServiceRequest module manages maritime service requests lifecycle: from creation through offer/assignment/work/completion to dispute resolution. Port: 7107.

## Prerequisites

### Environment Variables Required
| Variable | Value | Notes |
|---|---|---|
| service_request_api_base_url | http://localhost:7107/api/v1 | |
| service_request_api_root_url | http://localhost:7107 | |
| active_access_token | (set by auth) | |
| vesselId | (from Vessel module) | Required for creating service requests |
| currencyId | (from ReferenceData module) | Required for creating service requests |
| serviceRequestId | (auto-set) | Set by Create Service Request |
| serviceRequestOfferId | (auto-set) | Set by Create Offer |
| assignmentId | (auto-set) | Set by Create Assignment |

### Services Required
- Keycloak (port 8080)
- ServiceRequest API (port 7107)
- Vessel API (port 7105) — to get vesselId
- ReferenceData API (port 7104) — to get currencyId
- RabbitMQ (for async events)
- SignalR Hub (ws://localhost:7107/hubs/service-request)

## Auth Setup
1. Run `00 - Auth Setup > Get Mobile Token` — for owner operations
2. (In real scenario) Use a different user token for provider operations

## Pre-Test Setup
Before running ServiceRequest collection:
1. Run Vessel collection → create vessel → get `vesselId`
2. Run ReferenceData collection → get currencies → get `currencyId`
3. Return to ServiceRequest collection

## Test Sequence

### Complete Service Request Lifecycle
1. `01 - Service Request - CRUD > Create Service Request` → sets `serviceRequestId`
2. `01 - Service Request - CRUD > Get Service Request` → verify
3. `01 - Service Request - CRUD > Publish Service Request` → status changes to Published

### Offer Flow
4. `02 - Offers > Create Offer` → sets `serviceRequestOfferId` (provider perspective)
5. `02 - Offers > Accept Offer` → status changes to OfferAccepted (owner perspective)

### Assignment Flow
6. `03 - Assignment > Create Assignment` → sets `assignmentId`
7. `03 - Assignment > Accept Assignment` (provider accepts)
8. `03 - Assignment > Start Assignment` → status: InProgress

### Work Phase
9. `04 - Messages > Send Message` → sets `messageId`
10. `04 - Messages > Get Messages`
11. `05 - Work Logs > Add Work Log` → sets `workLogId`
12. `05 - Work Logs > Get Work Logs`

### Completion
13. `06 - Completion > Submit Completion` → sets `completionId`
14. `06 - Completion > Approve Completion` → status: Completed

### Dispute (alternative to Completion Approve)
13a. `07 - Dispute > Open Dispute` → sets `disputeId`
14a. `07 - Dispute > Change Dispute Status` (Admin)
15a. `07 - Dispute > Resolve Dispute` (Admin)

## Expected Responses

### Create Service Request
```json
HTTP 200 OK
{
  "id": "3fa85f64-...",
  "requestCode": "SR-2025-001",
  "status": "Draft",
  "title": "Engine Maintenance Required"
}
```

### Get Service Request
```json
HTTP 200 OK
{
  "id": "...",
  "title": "Engine Maintenance Required",
  "status": "Published",
  "serviceType": "EngineMaintenance",
  "vessel": { "id": "...", "name": "MV Test Vessel" },
  "offers": [],
  "assignment": null
}
```

## Common Errors

| Error | Cause | Fix |
|---|---|---|
| 401 Unauthorized | Missing token | Run Get Token |
| 404 Not Found | ID doesn't exist | Check variable values |
| 400 Invalid State | Wrong status for action | Check status flow |
| 403 Forbidden | Wrong actor | Use correct user token |
| 409 Conflict | Duplicate action | Check current state |

## Status Flow
```
Draft → Published → OfferAccepted → Assigned → InProgress → PendingCompletion → Completed
                                                                              ↘ Disputed → Resolved
         ↘ Cancelled
```
