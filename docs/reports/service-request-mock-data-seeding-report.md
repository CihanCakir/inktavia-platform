# ServiceRequest Mock Data Seeding Report

**Date**: 2026-06-14  
**Module**: ServiceRequest (`Aizen.Modules.ServiceRequest`)

---

## 1. Scope

Creates 12 demo service requests covering all major lifecycle states, with associated items, offers, assignments, work logs, completion records, dispute records, messages, and status history entries.

---

## 2. Files Created / Modified

| File | Action | Description |
|------|--------|-------------|
| `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/MockData/ServiceRequestMockDataSeeder.cs` | Created | Main seeder class |
| `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/MockData/MockDataSeedOptions.cs` | Created | Options class |
| `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/Json/MockData/admin-demo/service-requests.json` | Created | 12 service request definitions |
| `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/Json/MockData/admin-demo/service-request-items.json` | Created | Line items per request |
| `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/Json/MockData/admin-demo/service-request-status-history.json` | Created | Status transitions |
| `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/Json/MockData/admin-demo/service-request-offers.json` | Created | Provider offers |
| `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/Json/MockData/admin-demo/service-request-offer-items.json` | Created | Offer line items |
| `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/Json/MockData/admin-demo/service-request-assignments.json` | Created | Assignment records |
| `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/Json/MockData/admin-demo/service-request-worklogs.json` | Created | Work log entries |
| `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/Json/MockData/admin-demo/service-request-messages.json` | Created | Conversation messages |
| `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/Json/MockData/admin-demo/service-request-completions.json` | Created | Completion evidence records |
| `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/Json/MockData/admin-demo/service-request-disputes.json` | Created | Dispute records |
| `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/DependencyInjection.cs` | Modified | `AddServiceRequestMockData()`, calls seeder |
| `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest/Program.cs` | Modified | Calls `AddServiceRequestMockData()` |
| `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest/configuration/appsettings.json` | Modified | Added `MockData` (disabled) |
| `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest/configuration/appsettings.Local.json` | Modified | Added `MockData` (enabled) |

---

## 3. Demo Service Requests — Lifecycle Coverage

| SR ID | Status | Vessel | Owner | Provider | Entities Created |
|-------|--------|--------|-------|---------|-----------------|
| 30001 | Completed (41) | 20001 | 10003 | 10011 | Items, Offer, Assignment, WorkLog x2, Completion, Messages, StatusHistory x4 |
| 30002 | InProgress (30) | 20003 | 10007 | 10012 | Items, Offer, Assignment, WorkLog x1, Messages, StatusHistory x3 |
| 30003 | CompletionSubmitted (40) | 20002 | 10004 | 10011 | Items, Offer, Assignment, WorkLog x2, Completion (pending approval), Messages |
| 30004 | Scheduled (22) | 20007 | 10007 | 10012 | Items, Offer (accepted), Assignment, Messages |
| 30005 | WaitingForOffer (11) | 20005 | 10003 | — | Items, StatusHistory x2 |
| 30006 | DisputeOpened (50) | 20006 | 10004 | 10013 | Items, Offer, Assignment, WorkLog, Completion (rejected), Dispute, Messages, StatusHistory |
| 30007 | Completed (41) | 20004 | 10008 | 10011 | Items, Offer, Assignment, WorkLog, Completion (approved), Messages |
| 30008 | Draft (1) | 20008 | 10008 | — | Items |
| 30009 | OfferReceived (12) | 20001 | 10003 | 10012 | Items, Offer (submitted, awaiting acceptance), Messages |
| 30010 | Cancelled (90) | 20003 | 10007 | — | Items, StatusHistory x2 |
| 30011 | Scheduled (22) | 20007 | 10007 | 10013 | Items, Offer, Assignment |
| 30012 | InProgress (30) | 20005 | 10003 | 10013 | Items, Offer, Assignment, WorkLog |

---

## 4. Lifecycle States Covered

| Status Name | Status Code | Covered |
|-------------|------------|---------|
| Draft | 1 | ✅ SR 30008 |
| WaitingForOffer | 11 | ✅ SR 30005 |
| OfferReceived | 12 | ✅ SR 30009 |
| OfferAccepted | 13 | — (covered via transition to scheduled) |
| Scheduled / Assigned | 22 | ✅ SR 30004, 30011 |
| InProgress | 30 | ✅ SR 30002, 30012 |
| CompletionSubmitted | 40 | ✅ SR 30003 |
| Completed | 41 | ✅ SR 30001, 30007 |
| DisputeOpened | 50 | ✅ SR 30006 |
| Cancelled | 90 | ✅ SR 30010 |

---

## 5. Architecture Decisions

### 5.1 Seeding Order
Sub-entities are seeded in dependency order:
1. ServiceRequests
2. ServiceRequestItems
3. ServiceRequestOffers
4. ServiceRequestOfferItems
5. ServiceRequestAssignments
6. ServiceRequestWorkLogs
7. ServiceRequestCompletions
8. ServiceRequestDisputes
9. ServiceRequestMessages
10. ServiceRequestStatusHistories

### 5.2 Factory Method + Explicit ID Override
Same pattern as Vessel module. Factory methods ensure valid entity state, `entity.Id` is set to stable value after creation.

### 5.3 Reflection for Private Setters
Assignment `Status`, Completion `Status`, and Dispute `Status` use private setters. Reflection (`BindingFlags.NonPublic`) is used to set these for demo data.

---

## 6. Cross-Module References

| Field | Stable Value | References |
|-------|-------------|------------|
| `OwnerUserId` | 10003–10008 | Identity `UserEntity.Id` |
| `VesselId` | 20001–20008 | Vessel `VesselEntity.Id` |
| `ProviderUserId` | 10011–10013 | Identity `UserEntity.Id` |
| `ResolvedByAdminUserId` | 10001 | Identity Admin UserEntity.Id |

---

## 7. Idempotency Strategy

```csharp
var exists = await dbContext.ServiceRequests.AnyAsync(r => r.Id == model.Id, ct);
if (!exists) { ... insert ... }
```
Same `AnyAsync` guard on every entity type.

---

## 8. Validation Results

- **Build**: ✅ 0 errors
- **JSON files**: ✅ 12 JSON files in correct location
- **DI registration**: ✅ `AddServiceRequestMockData()` registered
- **Seeder integration**: ✅ Called in `SeedServiceRequestAsync()`

---

## 9. Remaining Gaps / Follow-ups

- `ServiceRequestAttachmentEntity` is not seeded (requires FileStorage FileIds).
- Message `AttachmentFileId` values are null/empty in mock data.
- WorkLog `AttachmentFileId` values are null/empty in mock data.
- Completion `EvidenceFileId` is null/empty in mock data.
- These can be populated in a future iteration once FileStorage module mock seeding is implemented.
- `Open` status (code 10) does not have a direct demo SR — WaitingForOffer (11) is the typical post-Open state; can be added if needed.
