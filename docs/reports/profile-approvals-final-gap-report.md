# Profile Approvals BFF — Final Gap Report
Generated: 2026-06-23

## Files Created / Modified

### Modified
| File | Change |
|---|---|
| `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/Common/RemoteClients/IIdentityAdminBffRemoteCall.cs` | Added 10 new interface methods + 2 new result wrapper classes |

### Created — Application Layer
| File | Lines |
|---|---|
| `AdminProfileApprovals/Dto/AdminProfileApprovalQueueBffResponse.cs` | 28 |
| `AdminProfileApprovals/Dto/ProfileApprovalQueueItemBffDto.cs` | 24 |
| `AdminProfileApprovals/Dto/ProfileApprovalSummaryBffDto.cs` | 14 |
| `AdminProfileApprovals/Dto/SharedApprovalBffDtos.cs` | 50 |
| `AdminProfileApprovals/Dto/OrganizerApprovalDetailBffResponse.cs` | 74 |
| `AdminProfileApprovals/Dto/VenueApprovalDetailBffResponse.cs` | 80 |
| `AdminProfileApprovals/Dto/ProfileApprovalDecisionBffResponse.cs` | 24 |
| `AdminProfileApprovals/Dto/RejectProfileApprovalBffRequest.cs` | 14 |
| `AdminProfileApprovals/Query/GetProfileApprovalQueueBffQuery.cs` | 38 |
| `AdminProfileApprovals/Query/GetProfileApprovalQueueBffQueryHandler.cs` | 196 |
| `AdminProfileApprovals/Query/GetOrganizerApprovalDetailBffQuery.cs` | 22 |
| `AdminProfileApprovals/Query/GetOrganizerApprovalDetailBffQueryHandler.cs` | 107 |
| `AdminProfileApprovals/Query/GetVenueApprovalDetailBffQuery.cs` | 22 |
| `AdminProfileApprovals/Query/GetVenueApprovalDetailBffQueryHandler.cs` | 107 |
| `AdminProfileApprovals/Command/ApproveOrganizerProfileBffCommand.cs` | 21 |
| `AdminProfileApprovals/Command/ApproveOrganizerProfileBffCommandHandler.cs` | 70 |
| `AdminProfileApprovals/Command/RejectOrganizerProfileBffCommand.cs` | 23 |
| `AdminProfileApprovals/Command/RejectOrganizerProfileBffCommandHandler.cs` | 82 |
| `AdminProfileApprovals/Command/ApproveVenueProfileBffCommand.cs` | 21 |
| `AdminProfileApprovals/Command/ApproveVenueProfileBffCommandHandler.cs` | 70 |
| `AdminProfileApprovals/Command/RejectVenueProfileBffCommand.cs` | 23 |
| `AdminProfileApprovals/Command/RejectVenueProfileBffCommandHandler.cs` | 82 |

### Created — Controller Layer
| File | Lines |
|---|---|
| `Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Controllers/V1/AdminProfileApprovalsController.cs` | 105 |

---

## BFF Endpoints Exposed

| Method | Route | Handler |
|---|---|---|
| GET | `/api/v1/admin-panel/users/profile-approvals` | `GetProfileApprovalQueueBffQueryHandler` |
| GET | `/api/v1/admin-panel/users/profile-approvals/organizers/{userId}/profiles/{profileId}` | `GetOrganizerApprovalDetailBffQueryHandler` |
| GET | `/api/v1/admin-panel/users/profile-approvals/venues/{userId}/profiles/{profileId}` | `GetVenueApprovalDetailBffQueryHandler` |
| POST | `/api/v1/admin-panel/users/profile-approvals/organizers/{userId}/profiles/{profileId}/approve` | `ApproveOrganizerProfileBffCommandHandler` |
| POST | `/api/v1/admin-panel/users/profile-approvals/organizers/{userId}/profiles/{profileId}/reject` | `RejectOrganizerProfileBffCommandHandler` |
| POST | `/api/v1/admin-panel/users/profile-approvals/venues/{userId}/profiles/{profileId}/approve` | `ApproveVenueProfileBffCommandHandler` |
| POST | `/api/v1/admin-panel/users/profile-approvals/venues/{userId}/profiles/{profileId}/reject` | `RejectVenueProfileBffCommandHandler` |

---

## Identity Endpoints Consumed (new methods)

| Method | Identity Route | BFF Remote Call Method |
|---|---|---|
| GET | `/api/v1/identity/organizers/profiles?approvalStatus=&pageIndex=&pageSize=` | `GetAdminOrganizerProfilesByStatus` |
| GET | `/api/v1/identity/organizers/profiles/{profileId}` | `GetAdminOrganizerProfileOnly` |
| GET | `/api/v1/identity/organizers/profiles/{profileId}/with-user` | `GetAdminOrganizerProfileWithUser` |
| GET | `/api/v1/identity/venues/profiles?approvalStatus=&pageIndex=&pageSize=` | `GetAdminVenueProfilesByStatus` |
| GET | `/api/v1/identity/venues/profiles/{profileId}` | `GetAdminVenueProfileOnly` |
| GET | `/api/v1/identity/venues/profiles/{profileId}/with-user` | `GetAdminVenueProfileWithUser` |
| POST | `/api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/approve` | `ApproveOrganizerProfileAdmin` |
| POST | `/api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/reject` | `RejectOrganizerProfileAdmin` |
| POST | `/api/v1/identity/admin/venues/{userId}/profiles/{profileId}/approve` | `ApproveVenueProfileAdmin` |
| POST | `/api/v1/identity/admin/venues/{userId}/profiles/{profileId}/reject` | `RejectVenueProfileAdmin` |

---

## DTO Field Mapping

### ProfileApprovalQueueItemBffDto (from OrganizerProfileListItemDto / VenueProfileListItemDto)
| BFF Field | Identity Field | Status |
|---|---|---|
| `userId` | `UserId` | ✅ Mapped |
| `profileId` | `Id` | ✅ Mapped |
| `profileType` | Hardcoded "organizer"/"venue" | ✅ Mapped |
| `applicantName` | `FirstName + LastName` | ✅ Mapped |
| `submittedAt` | `CreateDate` | ✅ Mapped |
| `status` | `ApprovalStatus` (lowercased) | ✅ Mapped |
| `statusLabel` | `ApprovalStatus` | ✅ Mapped |
| `companyOrVenueName` | — | ❌ Gap: not in Identity list DTO |
| `email` | — | ❌ Gap: not in Identity list DTO |
| `phone` | — | ❌ Gap: not in Identity list DTO |
| `city` | — | ❌ Gap: not in Identity list DTO |
| `country` | — | ❌ Gap: not in Identity list DTO |
| `reviewedAt` | — | ❌ Gap: not in Identity list DTO |
| `riskLevel` | — | ❌ Gap: no risk level in Identity |
| `documentCompletionPercent` | — | ❌ Gap: no document tracking in Identity |

### OrganizerApprovalDetailBffDto (from OrganizerProfileDetailDto + OrganizerProfileWithUserDetailDto)
| BFF Field | Identity Field | Status |
|---|---|---|
| `userId` | `UserId` | ✅ Mapped |
| `profileId` | `Id` | ✅ Mapped |
| `status` | `ApprovalStatus` (lowercased) | ✅ Mapped |
| `reviewedAt` | `ApprovedAt` / `RejectedAt` | ✅ Mapped (partial) |
| `rejectionReason` | `RejectReason` (detail-only endpoint) | ✅ Mapped |
| `applicant.fullName` | `FirstName + LastName` | ✅ Mapped |
| `applicant.email` | `Email` (with-user endpoint) | ✅ Mapped |
| `applicant.phone` | `PhoneNumber` (with-user endpoint) | ✅ Mapped |
| `applicant.avatarUrl` | `ProfilePhotoUrl` | ✅ Mapped |
| `applicant.registeredAt` | `UserCreatedAt` (with-user endpoint) | ✅ Mapped |
| `applicant.identityType` | `LoginType` (with-user endpoint) | ✅ Mapped |
| `reviewedBy` | — | ❌ Gap: not in Identity DTO |
| `rejectionCategory` | — | ❌ Gap: not in Identity DTO |
| `internalNote` | — | ❌ Gap: not in Identity DTO |
| `company.*` (all fields) | — | ❌ Gap: Identity OrganizerProfile has no company fields |
| `checklist.*` (all fields) | — | ❌ Gap: not computed (no data source) |
| `documents` | — | ❌ Gap: no document module integration |
| `riskSignals` | — | ❌ Gap: no risk scoring |
| `activity` | — | ❌ Gap: no activity log endpoint |

### VenueApprovalDetailBffDto (from VenueProfileDetailDto + VenueProfileWithUserDetailDto)
Same gaps as organizer. Additionally:
| BFF Field | Identity Field | Status |
|---|---|---|
| `venue.*` (all fields) | — | ❌ Gap: Identity VenueProfile has no venue-specific fields |
| `location.*` | — | ❌ Gap: no geolocation in Identity DTO |

---

## Auth / Token Forwarding

- BFF acquires Keycloak service token via `IAdminPanelBffKeycloakServiceTokenProvider`
- `Authorization: Bearer <serviceToken>` is constructed server-side, never forwarded from browser
- `X-Aizen-User-Token` is extracted from incoming request header and forwarded to all Identity calls
- Controller uses `[Authorize(Policy = "AdminPanelAccess")]`

---

## Reject Reason Validation

- `reason` validated BFF-side: min 10 chars, max 1000 chars (trimmed)
- Returns warning in `Warnings` array when invalid (handler returns empty response)
- `reasonCategory`, `internalNote`, `notifyUser` are accepted by BFF but **NOT forwarded to Identity** (Identity only accepts `reason`)
- **Gap**: `reasonCategory`, `internalNote`, `notifyUser` are silently discarded after logging

---

## Profile ID Type Note

- New `AdminProfileApprovals` endpoints use `long profileId` consistently (correct for Identity module)
- Existing `AdminIdentityController` endpoints (`/identity/organizers/.../profiles/{profileId:guid}`) use `Guid profileId` — this is a **known pre-existing inconsistency** in the BFF, not introduced by this change

---

## Remaining Gaps

1. **Company/venue-specific fields**: Identity `OrganizerProfileListItemDto`, `VenueProfileListItemDto`, and detail DTOs do not have company name, tax number, address, city, country, capacity, venue type, etc. These fields require Identity module updates.
2. **Email/phone in queue list**: `OrganizerProfileListItemDto` and `VenueProfileListItemDto` do not include `Email` or `PhoneNumber`. Showing them in the queue requires either the with-user endpoint per row (N+1, unacceptable) or Identity exposing these in the list endpoint.
3. **Risk signals / risk level**: Not available anywhere in Identity module.
4. **Documents**: No document management integration in BFF currently.
5. **Activity log**: No activity/audit log endpoint in Identity for profile events.
6. **ReviewedBy**: Not returned by Identity approve/reject endpoints (EmptyResult).
7. **ReviewedAt on decision**: Not returned by Identity approve/reject endpoints.
8. **`reasonCategory`, `internalNote`, `notifyUser`**: Identity reject endpoint only accepts `{ "reason": string }`.
9. **Summary approvedThisWeek/rejectedThisWeek**: Computed from current in-memory page only; not from full dataset.
10. **Queue pagination**: BFF-side pagination over merged in-memory results (max 200 per type). Not truly server-side paginated.
11. **Frontend endpoints.ts**: Not updated — file not found in this repository (likely a separate frontend repo).

---

## Build Result

```
0 Error(s)
586 Warning(s) — all pre-existing, none introduced by this change
```
