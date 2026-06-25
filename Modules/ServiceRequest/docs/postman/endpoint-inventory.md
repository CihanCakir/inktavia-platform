# ServiceRequest Module — Endpoint Inventory

**Module:** ServiceRequest  
**Port:** 7107  
**Base URL:** `{{service_request_api_base_url}}` = `http://localhost:7107/api/v1`  
**Auth:** `Authorization: Bearer {{active_access_token}}`  

---

## ServiceRequestController

**Route prefix:** `/api/v1/service-requests`  
**Tag:** ServiceRequest  
**Auth:** Bearer

| # | Method | Route | Request Body / Params | Response |
|---|---|---|---|---|
| 1 | POST | /api/v1/service-requests | CreateServiceRequestRequest | Returns serviceRequestId |
| 2 | PUT | /api/v1/service-requests/{serviceRequestId} | UpdateServiceRequestRequest | 200 OK |
| 3 | GET | /api/v1/service-requests/{serviceRequestId} | — | ServiceRequestDto |
| 4 | GET | /api/v1/service-requests/my | status, pageIndex, pageSize | PagedResult<ServiceRequestDto> |
| 5 | PATCH | /api/v1/service-requests/{serviceRequestId}/cancel | CancelServiceRequestRequest | 200 OK |
| 6 | PATCH | /api/v1/service-requests/{serviceRequestId}/publish | — | 200 OK |
| 7 | POST | /api/v1/service-requests/{serviceRequestId}/attachments | AddServiceRequestAttachmentRequest | Returns attachmentId |

**Sample CreateServiceRequestRequest:**
```json
{
  "vesselId": "{{vesselId}}",
  "title": "Engine Maintenance Required",
  "description": "Main engine requires overhaul after 10,000 hours of operation",
  "serviceType": "EngineMaintenance",
  "requiredPort": "Istanbul Haydarpasa",
  "scheduledAt": "2025-03-01T08:00:00Z",
  "budgetMin": 5000.00,
  "budgetMax": 15000.00,
  "currencyId": "{{currencyId}}",
  "attachments": []
}
```

**Sample UpdateServiceRequestRequest:**
```json
{
  "title": "Engine Maintenance Required - Updated",
  "description": "Main engine requires comprehensive overhaul",
  "scheduledAt": "2025-03-15T08:00:00Z",
  "budgetMin": 8000.00,
  "budgetMax": 20000.00
}
```

**Sample CancelServiceRequestRequest:**
```json
{
  "reason": "Service requirements changed"
}
```

**ServiceType enum values:** `EngineMaintenance`, `HullCleaning`, `ElectricalRepair`, `NavigationEquipment`, `SafetyEquipment`, `CargoHandling`, `PortServices`, `General`

---

## ServiceRequestOfferController

**Route prefix:** `/api/v1/service-requests/{serviceRequestId}/offers`  
**Tag:** ServiceRequest - Offers  
**Auth:** Bearer

| # | Method | Route | Request Body | Response |
|---|---|---|---|---|
| 1 | POST | /api/v1/service-requests/{serviceRequestId}/offers | CreateServiceRequestOfferRequest | Returns offerId |
| 2 | PUT | /api/v1/service-requests/{serviceRequestId}/offers/{offerId} | UpdateServiceRequestOfferRequest | 200 OK |
| 3 | PATCH | /api/v1/service-requests/{serviceRequestId}/offers/{offerId}/accept | AcceptServiceRequestOfferRequest | 200 OK |
| 4 | PATCH | /api/v1/service-requests/{serviceRequestId}/offers/{offerId}/reject | RejectServiceRequestOfferRequest | 200 OK |
| 5 | PATCH | /api/v1/service-requests/{serviceRequestId}/offers/{offerId}/withdraw | WithdrawServiceRequestOfferRequest | 200 OK |

**Sample CreateServiceRequestOfferRequest:**
```json
{
  "providerProfileId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "offeredPrice": 12500.00,
  "currency": "USD",
  "estimatedDays": 7,
  "notes": "We can perform the maintenance within 7 working days using OEM parts"
}
```

**Sample AcceptServiceRequestOfferRequest:**
```json
{
  "notes": "Accepted - please confirm start date"
}
```

**Sample RejectServiceRequestOfferRequest:**
```json
{
  "reason": "Price exceeds budget constraints"
}
```

---

## ServiceRequestAssignmentController

**Route prefix:** `/api/v1/service-requests/{serviceRequestId}/assignment`  
**Tag:** ServiceRequest - Assignment  
**Auth:** Bearer

| # | Method | Route | Request Body | Response |
|---|---|---|---|---|
| 1 | POST | /api/v1/service-requests/{serviceRequestId}/assignment | CreateServiceRequestAssignmentRequest | Returns assignmentId |
| 2 | PATCH | /api/v1/service-requests/{serviceRequestId}/assignment/{assignmentId}/accept | — | 200 OK |
| 3 | PATCH | /api/v1/service-requests/{serviceRequestId}/assignment/{assignmentId}/reject | RejectServiceRequestAssignmentRequest | 200 OK |
| 4 | PATCH | /api/v1/service-requests/{serviceRequestId}/assignment/{assignmentId}/start | — | 200 OK |

**Sample CreateServiceRequestAssignmentRequest:**
```json
{
  "offerId": "{{serviceRequestOfferId}}",
  "scheduledStartAt": "2025-03-01T08:00:00Z"
}
```

---

## ServiceRequestMessageController

**Route prefix:** `/api/v1/service-requests/{serviceRequestId}/messages`  
**Tag:** ServiceRequest - Messages  
**Auth:** Bearer

| # | Method | Route | Request Body / Params | Response |
|---|---|---|---|---|
| 1 | POST | /api/v1/service-requests/{serviceRequestId}/messages | SendServiceRequestMessageRequest | Returns messageId |
| 2 | GET | /api/v1/service-requests/{serviceRequestId}/messages | skip, take | List<ServiceRequestMessageDto> |
| 3 | PATCH | /api/v1/service-requests/{serviceRequestId}/messages/mark-read | MarkServiceRequestMessagesReadRequest | 200 OK |

**Sample SendServiceRequestMessageRequest:**
```json
{
  "content": "Could you provide more details about the engine model?",
  "attachmentFileIds": []
}
```

**Sample MarkServiceRequestMessagesReadRequest:**
```json
{
  "messageIds": ["3fa85f64-5717-4562-b3fc-2c963f66afa6"]
}
```

---

## ServiceRequestWorkLogController

**Route prefix:** `/api/v1/service-requests/{serviceRequestId}/work-logs`  
**Tag:** ServiceRequest - WorkLogs  
**Auth:** Bearer

| # | Method | Route | Request Body | Response |
|---|---|---|---|---|
| 1 | POST | /api/v1/service-requests/{serviceRequestId}/work-logs/assignment/{assignmentId} | AddServiceRequestWorkLogRequest | Returns workLogId |
| 2 | GET | /api/v1/service-requests/{serviceRequestId}/work-logs/assignment/{assignmentId} | — | List<WorkLogDto> |

**Sample AddServiceRequestWorkLogRequest:**
```json
{
  "workDate": "2025-03-01T00:00:00Z",
  "hoursWorked": 8.5,
  "description": "Completed engine disassembly and inspection"
}
```

---

## ServiceRequestCompletionController

**Route prefix:** `/api/v1/service-requests/{serviceRequestId}/completion`  
**Tag:** ServiceRequest - Completion  
**Auth:** Bearer

| # | Method | Route | Request Body | Response |
|---|---|---|---|---|
| 1 | POST | /api/v1/service-requests/{serviceRequestId}/completion/{assignmentId} | SubmitServiceRequestCompletionRequest | Returns completionId |
| 2 | PATCH | /api/v1/service-requests/{serviceRequestId}/completion/approve | ApproveServiceRequestCompletionRequest | 200 OK |
| 3 | PATCH | /api/v1/service-requests/{serviceRequestId}/completion/reject | RejectServiceRequestCompletionRequest | 200 OK |

**Sample SubmitServiceRequestCompletionRequest:**
```json
{
  "summary": "Engine overhaul completed successfully. All components replaced as required.",
  "completedAt": "2025-03-08T17:00:00Z",
  "fileIds": []
}
```

**Sample ApproveServiceRequestCompletionRequest:**
```json
{
  "notes": "Excellent work, engine running perfectly"
}
```

**Sample RejectServiceRequestCompletionRequest:**
```json
{
  "reason": "One issue still outstanding - starboard bearing needs attention"
}
```

---

## ServiceRequestDisputeController

**Route prefix:** `/api/v1/service-requests/{serviceRequestId}/dispute`  
**Tag:** ServiceRequest - Dispute  
**Auth:** Bearer / Admin

| # | Method | Route | Auth | Request Body | Response |
|---|---|---|---|---|---|
| 1 | POST | /api/v1/service-requests/{serviceRequestId}/dispute | Bearer | OpenServiceRequestDisputeRequest | Returns disputeId |
| 2 | PATCH | /api/v1/service-requests/{serviceRequestId}/dispute/{disputeId}/status | Admin | ChangeServiceRequestDisputeStatusRequest | 200 OK |
| 3 | PATCH | /api/v1/service-requests/{serviceRequestId}/dispute/{disputeId}/resolve | Admin | ResolveServiceRequestDisputeRequest | 200 OK |

**Sample OpenServiceRequestDisputeRequest:**
```json
{
  "reason": "ServiceQualityIssue",
  "description": "The engine repair was not completed to specification",
  "evidenceFileIds": []
}
```

**Sample ResolveServiceRequestDisputeRequest:**
```json
{
  "resolution": "PartialRefund",
  "notes": "Both parties agreed to a 20% refund for incomplete work"
}
```

---

## AdminServiceRequestController

**Route prefix:** `/api/v1/admin/service-requests`  
**Tag:** Admin - ServiceRequest  
**Auth:** Bearer (Admin)

| # | Method | Route | Params | Response |
|---|---|---|---|---|
| 1 | GET | /api/v1/admin/service-requests | status, vesselId, pageIndex, pageSize | PagedResult<ServiceRequestAdminDto> |
| 2 | GET | /api/v1/admin/service-requests/disputes | status, pageIndex, pageSize | PagedResult<DisputeAdminDto> |
| 3 | GET | /api/v1/admin/service-requests/{serviceRequestId} | — | ServiceRequestAdminDetailDto |

---

## ServiceRequest Status Flow

```
Draft → Published → OfferReceived → OfferAccepted → Assigned → InProgress → PendingCompletion → Completed
                                                             ↘ Disputed → DisputeResolved
                ↘ Cancelled
```

## ServiceRequest Lifecycle

1. **Owner** creates service request (status: Draft)
2. **Owner** publishes request (status: Published)
3. **Provider** creates offer
4. **Owner** accepts offer (status: OfferAccepted)
5. **Owner** creates assignment (status: Assigned)
6. **Provider** accepts assignment
7. **Provider** starts work (status: InProgress)
8. **Provider** adds work logs
9. **Provider** submits completion (status: PendingCompletion)
10. **Owner** approves completion (status: Completed) OR rejects
11. Either party can open a dispute → Admin resolves
