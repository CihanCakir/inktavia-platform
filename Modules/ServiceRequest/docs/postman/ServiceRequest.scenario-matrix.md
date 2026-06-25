# ServiceRequest — Scenario Coverage Matrix

## Scenario Summary

| # | Scenario | Folders | Actors | Endpoints Used | State Transitions |
|---|---|---|---|---|---|
| 1 | Full Lifecycle (Happy Path) | 01-09 | Owner, Provider, Admin | All main endpoints | Draft→Published→OfferAccepted→Assigned→InProgress→PendingCompletion→Completed |
| 2 | Dispute Resolution | 01-06, 10 | Owner, Provider, Admin | Dispute endpoints | InProgress→PendingCompletion→Disputed→DisputeResolved |
| 3 | Cancel Flow | 01-04 | Owner | Cancel endpoint | Draft/Published→Cancelled |
| 4 | Offer Rejection | 01-03, 05 | Owner, Provider | Reject offer | Published→rejected offer |
| 5 | Admin Operations | 11 | Admin | Admin list/detail | Read-only |

## Endpoint-to-Scenario Matrix

| Endpoint | Scenario 1 | Scenario 2 | Scenario 3 | Scenario 4 | Scenario 5 |
|---|---|---|---|---|---|
| POST /service-requests | ✅ | ✅ | ✅ | ✅ | — |
| PUT /service-requests/{id} | ✅ | — | — | — | — |
| GET /service-requests/{id} | ✅ | ✅ | ✅ | ✅ | — |
| GET /service-requests/my | ✅ | — | — | — | — |
| PATCH /service-requests/{id}/publish | ✅ | ✅ | ✅ | ✅ | — |
| PATCH /service-requests/{id}/cancel | — | — | ✅ | — | — |
| POST /service-requests/{id}/attachments | ✅ | — | — | — | — |
| POST /{id}/offers | ✅ | ✅ | — | ✅ | — |
| PUT /{id}/offers/{offerId} | — | — | — | — | — |
| PATCH /{id}/offers/{offerId}/accept | ✅ | ✅ | — | — | — |
| PATCH /{id}/offers/{offerId}/reject | — | — | — | ✅ | — |
| PATCH /{id}/offers/{offerId}/withdraw | — | — | — | — | — |
| POST /{id}/assignment | ✅ | ✅ | — | — | — |
| PATCH /{id}/assignment/{aId}/accept | ✅ | ✅ | — | — | — |
| PATCH /{id}/assignment/{aId}/reject | — | — | — | — | — |
| PATCH /{id}/assignment/{aId}/start | ✅ | ✅ | — | — | — |
| POST /{id}/messages | ✅ | ✅ | — | — | — |
| GET /{id}/messages | ✅ | ✅ | — | — | — |
| PATCH /{id}/messages/mark-read | ✅ | — | — | — | — |
| POST /{id}/work-logs/assignment/{aId} | ✅ | ✅ | — | — | — |
| GET /{id}/work-logs/assignment/{aId} | ✅ | ✅ | — | — | — |
| POST /{id}/completion/{aId} | ✅ | ✅ | — | — | — |
| PATCH /{id}/completion/approve | ✅ | — | — | — | — |
| PATCH /{id}/completion/reject | — | — | — | — | — |
| POST /{id}/dispute | — | ✅ | — | — | — |
| PATCH /{id}/dispute/{dId}/status | — | ✅ | — | — | ✅ |
| PATCH /{id}/dispute/{dId}/resolve | — | ✅ | — | — | ✅ |
| GET /admin/service-requests | — | — | — | — | ✅ |
| GET /admin/service-requests/disputes | — | — | — | — | ✅ |
| GET /admin/service-requests/{id} | — | — | — | — | ✅ |

## Actor-to-Action Matrix

| Action | Owner | Provider | Admin |
|---|---|---|---|
| Create Service Request | ✅ | — | — |
| Publish Service Request | ✅ | — | — |
| Cancel Service Request | ✅ | — | ✅ |
| Create Offer | — | ✅ | — |
| Update Offer | — | ✅ | — |
| Accept Offer | ✅ | — | — |
| Reject Offer | ✅ | — | — |
| Withdraw Offer | — | ✅ | — |
| Create Assignment | ✅ | — | — |
| Accept Assignment | — | ✅ | — |
| Reject Assignment | — | ✅ | — |
| Start Assignment | — | ✅ | — |
| Send Message | ✅ | ✅ | ✅ |
| Add Work Log | — | ✅ | — |
| Submit Completion | — | ✅ | — |
| Approve Completion | ✅ | — | — |
| Reject Completion | ✅ | — | — |
| Open Dispute | ✅ | ✅ | — |
| Change Dispute Status | — | — | ✅ |
| Resolve Dispute | — | — | ✅ |
| List All Service Requests | — | — | ✅ |
| View Any Service Request | — | — | ✅ |

## State Transition Coverage

| From State | To State | Action | Covered in Collection |
|---|---|---|---|
| (none) | Draft | Create Service Request | ✅ Folder 04 Step 1 |
| Draft | Published | Publish | ✅ Folder 04 Step 2 |
| Draft | Cancelled | Cancel | ✅ Folder 01-CRUD |
| Published | Cancelled | Cancel | ✅ Folder 01-CRUD |
| Published | OfferReceived | Create Offer | ✅ Folder 05 |
| OfferReceived | OfferAccepted | Accept Offer | ✅ Folder 05 |
| OfferReceived | Published | Reject All Offers | Partial |
| OfferAccepted | Assigned | Create Assignment + Accept | ✅ Folder 06 |
| Assigned | InProgress | Start Assignment | ✅ Folder 06 |
| InProgress | PendingCompletion | Submit Completion | ✅ Folder 09 |
| PendingCompletion | Completed | Approve Completion | ✅ Folder 09 |
| PendingCompletion | InProgress | Reject Completion | Partial |
| Any | Disputed | Open Dispute | ✅ Folder 10 |
| Disputed | DisputeResolved | Resolve Dispute | ✅ Folder 10 |

## SignalR Event Coverage

| EventType | Triggered By | Covered |
|---|---|---|
| ServiceRequestCreated (1) | POST /service-requests | ✅ Folder 04 |
| ServiceRequestStatusChanged (3) | Publish/Cancel/etc | ✅ Multiple |
| OfferCreated (10) | POST /offers | ✅ Folder 05 |
| OfferAccepted (12) | PATCH /offers/{id}/accept | ✅ Folder 05 |
| AssignmentCreated (20) | POST /assignment | ✅ Folder 06 |
| AssignmentAccepted (22) | PATCH /assignment/{id}/accept | ✅ Folder 06 |
| MessageSent (30) | POST /messages | ✅ Folder 07 |
| WorkLogAdded (40) | POST /work-logs/... | ✅ Folder 08 |
| WorkStarted (41) | PATCH /assignment/{id}/start | ✅ Folder 06 |
| CompletionSubmitted (50) | POST /completion/{aId} | ✅ Folder 09 |
| CompletionApproved (51) | PATCH /completion/approve | ✅ Folder 09 |
| DisputeOpened (60) | POST /dispute | ✅ Folder 10 |
| DisputeResolved (62) | PATCH /dispute/{id}/resolve | ✅ Folder 10 |
