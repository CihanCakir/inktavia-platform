# APPROVALS_BFF_IMPLEMENTATION_PROMPT.md
# Copilot Agent Prompt — Organizer & Venue Profile Approval BFF Endpoints

Paste this entire file into a Copilot Agent session opened in the **Aizen.AdminPanel.BFF** project root.

---

## Context

You are implementing the AdminPanel BFF layer for Inktavia Marine OS **"Organizer & Venue Profile Approval"** screens.

The frontend pages involved are:
- `ApprovalsQueuePage` — combined pending queue (organizers + venues merged)
- `OrganizerReviewDetailPage` — full organizer profile review with documents, checklist, risk signals
- `VenueReviewDetailPage` — full venue profile review with location, documents, checklist
- `RejectionModal` — structured rejection with category + reason + internal note
- `ApprovalDecisionBanner` — approved / rejected state display

The Identity module already exposes approval/rejection endpoints at:

```http
POST /api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/approve
POST /api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/reject

POST /api/v1/identity/admin/venues/{userId}/profiles/{profileId}/approve
POST /api/v1/identity/admin/venues/{userId}/profiles/{profileId}/reject
```

Reject body (Identity accepts):
```json
{ "reason": "string" }
```

All user/profile IDs in the Identity module are `long`. Do NOT introduce `Guid` IDs for business entities.

The React Admin Web must **never** call Identity directly. It calls AdminPanel BFF only. The BFF then calls Identity using the existing Aizen service-token forwarding model.

---

## STEP 0 — Audit existing project patterns (MANDATORY before writing code)

Run all commands and read the output before creating any file:

```bash
# 1. Find existing BFF controllers to understand routing prefix and base class
find Bff/src/AdminPanel -name "*.cs" | grep -E "AdminUsers|AdminVessels|AdminServiceRequests|Controller|RemoteCall|Dto|Query|Command" | sort

# 2. Find AdminPanelAccess policy usage
grep -r "AdminPanelAccess" Bff/src/AdminPanel --include="*.cs" | head -10

# 3. Find IIdentityAdminBffRemoteCall — this is the interface you will extend
grep -r "IIdentityAdminBffRemoteCall" Bff/src/AdminPanel --include="*.cs" | head -20

# 4. Find the Aizen response envelope pattern
grep -r "AizenBffResponse\|AizenApiResponse\|Warnings\|AdminBffWarning" Bff/src/AdminPanel --include="*.cs" | head -20

# 5. Find the Keycloak service token provider
grep -r "IAdminPanelBffKeycloakServiceTokenProvider" Bff/src/AdminPanel --include="*.cs" | head -10

# 6. Find existing RemoteCall attribute convention
grep -r "AizenRemoteCallPost\|AizenRemoteCallGet\|AizenRemoteCallHeader" Bff/src/AdminPanel --include="*.cs" | head -20

# 7. Find existing approve/reject endpoints in Identity (confirm exact routes)
grep -r "ApproveOrganizerProfileCommand\|RejectOrganizerProfileCommand\|ApproveVenueProfileCommand\|RejectVenueProfileCommand" Modules/Identity/src --include="*.cs"

# 8. Find Identity organizer/venue list and detail endpoints
grep -r "Organizer" Modules/Identity/src/Aizen.Modules.Identity* --include="*.cs" | grep -i "controller\|endpoint\|route" | head -30
grep -r "Venue" Modules/Identity/src/Aizen.Modules.Identity* --include="*.cs" | grep -i "controller\|endpoint\|route" | head -30

# 9. Confirm profile status enum values
grep -r "ProfileStatus\|OrganizerProfileStatus\|VenueProfileStatus" Modules/Identity/src --include="*.cs" | head -20

# 10. Confirm response DTO shapes for organizer/venue list and detail
grep -r "OrganizerProfileDto\|OrganizerListResponse\|VenueProfileDto\|VenueListResponse" Modules/Identity/src --include="*.cs" | head -20
```

After running discovery, confirm:
- Controller base class and `[Route]` attribute convention
- Whether responses are wrapped in `{ header: { isSuccess, errorCode }, body }` envelope or `Ok(dto)`
- How `IIdentityAdminBffRemoteCall` is structured and injected
- Whether Identity already has organizer/venue list+detail endpoints (if not, document as gap)
- Profile status enum: which values exist (`Pending`, `Approved`, `Rejected`, `Suspended`, `Active`)
- Exact field names on `OrganizerProfile` and `VenueProfile` entities

---

## STEP 1 — Audit Identity module endpoints

```bash
grep -r "Get.*Organizer\|Get.*Venue\|Organizer.*List\|Venue.*List\|ByFilter\|GetAll" \
  Modules/Identity/src --include="*.cs" | head -40
```

If Identity **does not** expose list/detail endpoints for organizer/venue profiles, do NOT invent fake data. Create a gap entry in the final report and return empty arrays with a warning. The UI handles empty state gracefully.

If Identity **does** expose them, use them via `IIdentityAdminBffRemoteCall`.

---

## STEP 2 — Frontend endpoint alignment (MANDATORY)

The React Admin Web currently calls Identity-like routes through the BFF base URL. After implementing the BFF, update the frontend:

File: `src/shared/api/endpoints.ts`

Replace the current `/identity/*` approval endpoints with the new BFF-aligned routes:

```typescript
// BEFORE (incorrect — calls Identity paths directly via BFF base)
IDENTITY_ORGANIZER_PROFILES: '/identity/organizers/profiles',
IDENTITY_ORGANIZER_PROFILES_BY_ID: (profileId: string) => `/identity/organizers/profiles/${profileId}`,
IDENTITY_ORGANIZER_PROFILES_WITH_USER: (profileId: string) => `/identity/organizers/profiles/${profileId}/with-user`,
IDENTITY_ORGANIZER_APPROVE: (userId: string, profileId: string) => `/identity/organizers/${userId}/profiles/${profileId}/approve`,
IDENTITY_ORGANIZER_REJECT: (userId: string, profileId: string) => `/identity/organizers/${userId}/profiles/${profileId}/reject`,
IDENTITY_VENUE_PROFILES: '/identity/venues/profiles',
IDENTITY_VENUE_PROFILES_BY_ID: (profileId: string) => `/identity/venues/profiles/${profileId}`,
IDENTITY_VENUE_APPROVE: (userId: string, profileId: string) => `/identity/venues/${userId}/profiles/${profileId}/approve`,
IDENTITY_VENUE_REJECT: (userId: string, profileId: string) => `/identity/venues/${userId}/profiles/${profileId}/reject`,

// AFTER (correct — all approval traffic goes through BFF)
PROFILE_APPROVALS_QUEUE: '/users/profile-approvals',
PROFILE_APPROVALS_ORGANIZER_DETAIL: (userId: string, profileId: string) =>
  `/users/profile-approvals/organizers/${userId}/profiles/${profileId}`,
PROFILE_APPROVALS_VENUE_DETAIL: (userId: string, profileId: string) =>
  `/users/profile-approvals/venues/${userId}/profiles/${profileId}`,
PROFILE_APPROVALS_ORGANIZER_APPROVE: (userId: string, profileId: string) =>
  `/users/profile-approvals/organizers/${userId}/profiles/${profileId}/approve`,
PROFILE_APPROVALS_ORGANIZER_REJECT: (userId: string, profileId: string) =>
  `/users/profile-approvals/organizers/${userId}/profiles/${profileId}/reject`,
PROFILE_APPROVALS_VENUE_APPROVE: (userId: string, profileId: string) =>
  `/users/profile-approvals/venues/${userId}/profiles/${profileId}/approve`,
PROFILE_APPROVALS_VENUE_REJECT: (userId: string, profileId: string) =>
  `/users/profile-approvals/venues/${userId}/profiles/${profileId}/reject`,
```

> IDs are `string` in the frontend because TypeScript JSON deserialization maps JSON numbers to `number`, and large `long` values can exceed JS `Number.MAX_SAFE_INTEGER`. If Identity IDs fit within safe integer range, `string` serialization on the BFF is also acceptable. Confirm with the team and follow the existing BFF convention for other long IDs.

---

## STEP 3 — Target BFF endpoint contract

All BFF endpoints live under: `GET|POST /api/v1/admin-panel/users/profile-approvals`

### 3.1 — Approval Queue (combined organizer + venue)

```http
GET /api/v1/admin-panel/users/profile-approvals
```

Query params:
```
pageIndex      int?    default 0
pageSize       int?    default 20
searchTerm     string? free-text on name / company / email
profileType    string? "organizer" | "venue" | "all"
status         string? "pending" | "approved" | "rejected" | "all"
submittedFrom  string? ISO 8601 date
submittedTo    string? ISO 8601 date
riskLevel      string? "low" | "medium" | "high"
```

Response:
```typescript
type AdminProfileApprovalQueueBffResponse = {
  approvals: {
    from: number;
    index: number;
    size: number;
    count: number;
    pages: number;
    hasPrevious: boolean;
    hasNext: boolean;
    items: ProfileApprovalQueueItemBffDto[];
  } | null;
  summary: ProfileApprovalSummaryBffDto;
  warnings: AdminBffWarning[];
};

type ProfileApprovalQueueItemBffDto = {
  userId: number;
  profileId: number;
  profileType: "organizer" | "venue";
  applicantName: string | null;
  companyOrVenueName: string | null;
  email: string | null;
  phone: string | null;
  city: string | null;
  country: string | null;
  submittedAt: string | null;          // ISO 8601 UTC
  reviewedAt: string | null;
  status: "pending" | "approved" | "rejected" | "needsReview";
  statusLabel: string;
  riskLevel: "low" | "medium" | "high" | null;   // maps from Identity L/M/H
  documentCompletionPercent: number | null;
};

type ProfileApprovalSummaryBffDto = {
  pendingOrganizers: number;
  pendingVenues: number;
  approvedThisWeek: number;
  rejectedThisWeek: number;
  averageReviewTimeHours: number | null;
};
```

### 3.2 — Organizer Review Detail

```http
GET /api/v1/admin-panel/users/profile-approvals/organizers/{userId}/profiles/{profileId}
```

Response:
```typescript
type OrganizerApprovalDetailBffResponse = {
  organizer: OrganizerApprovalDetailBffDto | null;
  warnings: AdminBffWarning[];
};

type OrganizerApprovalDetailBffDto = {
  userId: number;
  profileId: number;
  status: "pending" | "approved" | "rejected";
  reviewedBy: string | null;
  reviewedAt: string | null;
  rejectionCategory: string | null;
  rejectionReason: string | null;
  internalNote: string | null;

  applicant: {
    fullName: string | null;
    email: string | null;
    phone: string | null;
    avatarUrl: string | null;
    registeredAt: string | null;
    identityType: string | null;
    role: string | null;
  };

  company: {
    companyName: string | null;
    taxNumber: string | null;
    taxOffice: string | null;
    address: string | null;
    city: string | null;
    country: string | null;
    website: string | null;
    contactPerson: string | null;
    businessCategory: string | null;
    businessLicenseNo: string | null;
    operationalScore: number | null;
    estimatedRevenue: string | null;
    hqLocation: string | null;
  };

  checklist: {
    emailVerified: boolean;
    phoneVerified: boolean;
    companyNameProvided: boolean;
    taxNumberProvided: boolean;
    documentsUploaded: boolean;
    duplicateAccountFound: boolean;
    suspiciousActivityFound: boolean;
  };

  documents: ProfileApprovalDocumentBffDto[];
  riskSignals: ProfileApprovalRiskSignalBffDto[];
  activity: ProfileApprovalActivityItemBffDto[];
  warnings: AdminBffWarning[];    // per-section warnings
};

type ProfileApprovalDocumentBffDto = {
  id: string;
  type: string;
  name: string;
  fileId: string;
  uploadedAt: string;
  format: string | null;       // "PDF" | "PNG" | "JPG"
  size: string | null;         // "4.2 MB"
  issuer: string | null;
  matchScore: string | null;   // "98.4%"
};

type ProfileApprovalRiskSignalBffDto = {
  level: "high" | "medium" | "low";
  title: string;
  description: string;
};

type ProfileApprovalActivityItemBffDto = {
  eventType: string;
  description: string;
  performedBy: string | null;
  performedAt: string;
};
```

### 3.3 — Venue Review Detail

```http
GET /api/v1/admin-panel/users/profile-approvals/venues/{userId}/profiles/{profileId}
```

Response:
```typescript
type VenueApprovalDetailBffResponse = {
  venue: VenueApprovalDetailBffDto | null;
  warnings: AdminBffWarning[];
};

type VenueApprovalDetailBffDto = {
  userId: number;
  profileId: number;
  status: "pending" | "approved" | "rejected";
  reviewedBy: string | null;
  reviewedAt: string | null;
  rejectionCategory: string | null;
  rejectionReason: string | null;
  internalNote: string | null;

  owner: {
    fullName: string | null;
    email: string | null;
    phone: string | null;
    avatarUrl: string | null;
    registeredAt: string | null;
  };

  venue: {
    venueName: string | null;
    venueType: string | null;
    capacity: number | null;
    address: string | null;
    city: string | null;
    country: string | null;
    contactPerson: string | null;
    businessRegistrationNo: string | null;
    taxNo: string | null;
    website: string | null;
    operationalHours: string | null;
    securityTier: number | null;
    memberId: string | null;
    rating: number | null;
    eventCount: number | null;
    revenue: string | null;
  };

  location: {
    latitude: number | null;
    longitude: number | null;
    addressVerified: boolean;
    displayText: string | null;
  };

  checklist: {
    emailVerified: boolean;
    phoneVerified: boolean;
    venueNameProvided: boolean;
    taxNumberProvided: boolean;
    addressProvided: boolean;
    documentsUploaded: boolean;
    duplicateProfileFound: boolean;
  };

  documents: ProfileApprovalDocumentBffDto[];
  riskSignals: ProfileApprovalRiskSignalBffDto[];
  activity: ProfileApprovalActivityItemBffDto[];
};
```

### 3.4 — Approve Organizer

```http
POST /api/v1/admin-panel/users/profile-approvals/organizers/{userId}/profiles/{profileId}/approve
```

No request body required.

Response:
```typescript
type ProfileApprovalDecisionBffResponse = {
  decision: {
    userId: number;
    profileId: number;
    profileType: "organizer" | "venue";
    status: "approved" | "rejected";
    reviewedAt: string | null;
    reviewedByName: string | null;
  };
  warnings: AdminBffWarning[];
};
```

BFF calls Identity:
```http
POST /api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/approve
```

### 3.5 — Reject Organizer

```http
POST /api/v1/admin-panel/users/profile-approvals/organizers/{userId}/profiles/{profileId}/reject
```

Request:
```typescript
type RejectProfileApprovalBffRequest = {
  reason: string;                   // required, min 10 chars, max 1000 chars
  reasonCategory?:
    | "missingDocuments"
    | "invalidCompanyInfo"
    | "duplicateProfile"
    | "verificationFailed"
    | "invalidTaxId"
    | "addressMismatch"
    | "incompleteApplication"
    | "other";
  internalNote?: string | null;
  notifyUser?: boolean;             // default true
};
```

BFF forwards to Identity (only what Identity supports):
```json
{ "reason": "string" }
```

`reasonCategory`, `internalNote`, `notifyUser` — if Identity does not support these fields, document in gap report. Do not silently discard or fabricate persistence.

BFF calls Identity:
```http
POST /api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/reject
```

### 3.6 — Approve Venue

```http
POST /api/v1/admin-panel/users/profile-approvals/venues/{userId}/profiles/{profileId}/approve
```

BFF calls Identity:
```http
POST /api/v1/identity/admin/venues/{userId}/profiles/{profileId}/approve
```

### 3.7 — Reject Venue

```http
POST /api/v1/admin-panel/users/profile-approvals/venues/{userId}/profiles/{profileId}/reject
```

Same request body as organizer reject. BFF calls Identity:
```http
POST /api/v1/identity/admin/venues/{userId}/profiles/{profileId}/reject
```

---

## STEP 4 — Extend IIdentityAdminBffRemoteCall

Extend the existing interface:
```
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/Common/RemoteClients/IIdentityAdminBffRemoteCall.cs
```

Add methods following the existing convention in this file:

```csharp
// Organizer profiles — list
[AizenRemoteCallGet("/api/v1/identity/admin/organizers/profiles")]
Task<AizenApiResponse<OrganizerProfileListIdentityResponse>> GetOrganizerProfilesAsync(
    [AizenRemoteCallHeader("Authorization")] string authorization,
    [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
    [AizenRemoteCallQuery("status")] string? status,
    [AizenRemoteCallQuery("pageIndex")] int pageIndex,
    [AizenRemoteCallQuery("pageSize")] int pageSize);

// Organizer profile — detail with user info
[AizenRemoteCallGet("/api/v1/identity/admin/organizers/{userId}/profiles/{profileId}")]
Task<AizenApiResponse<OrganizerProfileDetailIdentityResponse>> GetOrganizerProfileDetailAsync(
    [AizenRemoteCallHeader("Authorization")] string authorization,
    [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
    long userId,
    long profileId);

// Organizer approve
[AizenRemoteCallPost("/api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/approve")]
Task<AizenApiResponse<OrganizerProfileDecisionIdentityResponse>> ApproveOrganizerProfileAsync(
    [AizenRemoteCallHeader("Authorization")] string authorization,
    [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
    long userId,
    long profileId);

// Organizer reject
[AizenRemoteCallPost("/api/v1/identity/admin/organizers/{userId}/profiles/{profileId}/reject")]
Task<AizenApiResponse<OrganizerProfileDecisionIdentityResponse>> RejectOrganizerProfileAsync(
    [AizenRemoteCallHeader("Authorization")] string authorization,
    [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
    long userId,
    long profileId,
    RejectProfileIdentityRequest request);

// Venue profiles — list
[AizenRemoteCallGet("/api/v1/identity/admin/venues/profiles")]
Task<AizenApiResponse<VenueProfileListIdentityResponse>> GetVenueProfilesAsync(
    [AizenRemoteCallHeader("Authorization")] string authorization,
    [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
    [AizenRemoteCallQuery("status")] string? status,
    [AizenRemoteCallQuery("pageIndex")] int pageIndex,
    [AizenRemoteCallQuery("pageSize")] int pageSize);

// Venue profile — detail
[AizenRemoteCallGet("/api/v1/identity/admin/venues/{userId}/profiles/{profileId}")]
Task<AizenApiResponse<VenueProfileDetailIdentityResponse>> GetVenueProfileDetailAsync(
    [AizenRemoteCallHeader("Authorization")] string authorization,
    [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
    long userId,
    long profileId);

// Venue approve
[AizenRemoteCallPost("/api/v1/identity/admin/venues/{userId}/profiles/{profileId}/approve")]
Task<AizenApiResponse<VenueProfileDecisionIdentityResponse>> ApproveVenueProfileAsync(
    [AizenRemoteCallHeader("Authorization")] string authorization,
    [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
    long userId,
    long profileId);

// Venue reject
[AizenRemoteCallPost("/api/v1/identity/admin/venues/{userId}/profiles/{profileId}/reject")]
Task<AizenApiResponse<VenueProfileDecisionIdentityResponse>> RejectVenueProfileAsync(
    [AizenRemoteCallHeader("Authorization")] string authorization,
    [AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken,
    long userId,
    long profileId,
    RejectProfileIdentityRequest request);
```

> **Adjust attribute names and method signatures to exactly match the existing methods in this file.**
> Read `IIdentityAdminBffRemoteCall.cs` before writing anything.

Also add the Identity-side request/response contracts that are missing:

```csharp
// RejectProfileIdentityRequest.cs
public record RejectProfileIdentityRequest(string Reason);
```

---

## STEP 5 — Create BFF DTOs

Create folder:
```
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminProfileApprovals/Dto/
```

Files:
```
AdminProfileApprovalQueueBffResponse.cs
ProfileApprovalQueueItemBffDto.cs
ProfileApprovalSummaryBffDto.cs
OrganizerApprovalDetailBffResponse.cs
OrganizerApprovalDetailBffDto.cs
OrganizerApplicantBffDto.cs
OrganizerCompanyBffDto.cs
VenueApprovalDetailBffResponse.cs
VenueApprovalDetailBffDto.cs
VenueOwnerBffDto.cs
VenueDetailBffDto.cs
VenueLocationBffDto.cs
ProfileApprovalChecklistBffDto.cs
ProfileApprovalDocumentBffDto.cs
ProfileApprovalRiskSignalBffDto.cs
ProfileApprovalActivityItemBffDto.cs
ProfileApprovalDecisionBffResponse.cs
RejectProfileApprovalBffRequest.cs
```

Use `long` for `userId` / `profileId`. All response types must include `AdminBffWarning[] Warnings` if that is the established BFF pattern.

---

## STEP 6 — Implement BFF Query / Command handlers

Create feature folder:
```
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminProfileApprovals/
```

```
Query/GetProfileApprovalQueueBffQuery.cs
Query/GetProfileApprovalQueueBffQueryHandler.cs

Query/GetOrganizerApprovalDetailBffQuery.cs
Query/GetOrganizerApprovalDetailBffQueryHandler.cs

Query/GetVenueApprovalDetailBffQuery.cs
Query/GetVenueApprovalDetailBffQueryHandler.cs

Command/ApproveOrganizerProfileBffCommand.cs
Command/ApproveOrganizerProfileBffCommandHandler.cs

Command/RejectOrganizerProfileBffCommand.cs
Command/RejectOrganizerProfileBffCommandHandler.cs

Command/ApproveVenueProfileBffCommand.cs
Command/ApproveVenueProfileBffCommandHandler.cs

Command/RejectVenueProfileBffCommand.cs
Command/RejectVenueProfileBffCommandHandler.cs
```

Every handler must:
1. Acquire BFF service token via `IAdminPanelBffKeycloakServiceTokenProvider`
2. Build `Authorization: Bearer <serviceToken>` header
3. Forward incoming `X-Aizen-User-Token` without modification
4. Call the appropriate `IIdentityAdminBffRemoteCall` method
5. Check `AizenApiResponse.Header.IsSuccess` — do NOT treat Identity failure as BFF success
6. Map Identity DTO → BFF DTO
7. Collect warnings from Identity response, add any BFF-side warnings
8. Never swallow exceptions silently; log as warning and add to `Warnings` array
9. For the queue handler: fetch organizers and venues **in parallel** (`Task.WhenAll`) — if one fails, add warning and return what is available

---

## STEP 7 — Queue aggregation strategy

```
GetProfileApprovalQueueBffQueryHandler:

1. Fetch organizers and venues in parallel:
   Task orgTask = IIdentityAdminBffRemoteCall.GetOrganizerProfilesAsync(...)
   Task venTask = IIdentityAdminBffRemoteCall.GetVenueProfilesAsync(...)
   await Task.WhenAll(orgTask, venTask)

2. If orgTask faulted → add AdminBffWarning, continue with venues only
   If venTask faulted → add AdminBffWarning, continue with organizers only

3. Normalize each Identity item to ProfileApprovalQueueItemBffDto:
   - Map RiskLevel: L → "low", M → "medium", H → "high"
   - Map Status: case-insensitive "Pending" → "pending", etc.
   - Set profileType: "organizer" | "venue"
   - Compute companyOrVenueName: organizationName OR venueName
   - Compute applicantName: firstName + lastName, or email prefix, or organizationName

4. Apply BFF-side filters (if Identity does not support them):
   - profileType filter
   - searchTerm (ILIKE on applicantName, companyOrVenueName, email)
   - submittedFrom / submittedTo date range
   - riskLevel

5. Sort merged list by submittedAt descending

6. Page the combined result:
   var paged = merged.Skip(query.PageIndex * query.PageSize).Take(query.PageSize)

7. Compute summary from the FULL (pre-paged) merged list:
   pendingOrganizers = orgs where status == "pending" count
   pendingVenues = venues where status == "pending" count
   approvedThisWeek = all where status == "approved" and reviewedAt >= 7 days ago
   rejectedThisWeek = all where status == "rejected" and reviewedAt >= 7 days ago

8. If Identity supports status filter server-side, prefer that over BFF-side filtering.
   Do not make per-row detail calls for queue rows.
```

---

## STEP 8 — Detail screen mapping

### Organizer entity → OrganizerApprovalDetailBffDto

| BFF field | Identity entity field | Rule |
|---|---|---|
| `userId` | `UserId` | `long` |
| `profileId` | `ProfileId` (or `Id`) | `long` — discover actual PK name |
| `status` | `Status.ToString().ToLower()` | |
| `reviewedBy` | `ReviewedBy` | |
| `reviewedAt` | `ReviewedAt?.ToString("O")` | UTC ISO 8601 |
| `rejectionCategory` | `RejectionCategory?.ToString()` | |
| `rejectionReason` | `RejectionReason` | |
| `internalNote` | `InternalNote` | |
| `applicant.fullName` | `FirstName + " " + LastName` | trim; null if both missing |
| `applicant.email` | `Email` | |
| `applicant.phone` | `Phone` | |
| `company.companyName` | `OrganizationName` | |
| `company.taxNumber` | `TaxNumber` | |
| `company.address` | `Address` | |
| `company.businessLicenseNo` | `BusinessLicenseNo` | |
| `company.operationalScore` | `OperationalScore` | |
| `company.estimatedRevenue` | `EstimatedRevenue` | |
| `company.hqLocation` | `HqLocation` | |
| `checklist.companyNameProvided` | `OrganizationName != null` | compute |
| `checklist.taxNumberProvided` | `TaxNumber != null` | compute |
| `checklist.documentsUploaded` | `Documents?.Count > 0` | compute |
| `checklist.emailVerified` | map if available; `false` otherwise | |
| `checklist.duplicateAccountFound` | `false` unless available | |
| `documents` | `Documents.Select(MapDocument)` | return `[]` if null |
| `riskSignals` | `RiskSignals.Select(MapSignal)` | return `[]` if null |

### Venue entity → VenueApprovalDetailBffDto

| BFF field | Identity entity field | Rule |
|---|---|---|
| `venue.venueName` | `VenueName` | |
| `venue.venueType` | `VenueType` | |
| `venue.capacity` | `Capacity` | |
| `venue.memberId` | `MemberId` | e.g. `#INK-8829-VX` |
| `venue.operationalHours` | `OperationalHours` | |
| `venue.securityTier` | `SecurityTier` | |
| `venue.rating` | `Rating` | |
| `venue.eventCount` | `EventCount` | |
| `venue.revenue` | `Revenue` | |
| `location.latitude` | `Latitude` | |
| `location.longitude` | `Longitude` | |
| `location.displayText` | `OfficialAddress` or `Location` | |
| `location.addressVerified` | `OfficialAddress != null` | compute |
| `owner.fullName` | `OwnerName` | |
| `checklist.venueNameProvided` | `VenueName != null` | compute |
| `checklist.addressProvided` | `OfficialAddress != null` | compute |
| `checklist.documentsUploaded` | `Documents?.Count > 0` | compute |

---

## STEP 9 — Implement controller

Create:
```
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Controllers/V1/AdminProfileApprovalsController.cs
```

```csharp
[ApiController]
[Route("api/v1/admin-panel/users/profile-approvals")]
[Authorize(Policy = "AdminPanelAccess")]
public sealed class AdminProfileApprovalsController : ControllerBase
{
    // Read existing BFF controllers before writing this class.
    // Use the EXACT base class, ISender injection, X-Aizen-User-Token extraction,
    // and response-wrapping pattern already in use.

    [HttpGet]
    public Task<IActionResult> GetQueue([FromQuery] GetProfileApprovalQueueBffQuery query, ...) { }

    [HttpGet("organizers/{userId:long}/profiles/{profileId:long}")]
    public Task<IActionResult> GetOrganizerDetail(long userId, long profileId, ...) { }

    [HttpGet("venues/{userId:long}/profiles/{profileId:long}")]
    public Task<IActionResult> GetVenueDetail(long userId, long profileId, ...) { }

    [HttpPost("organizers/{userId:long}/profiles/{profileId:long}/approve")]
    public Task<IActionResult> ApproveOrganizer(long userId, long profileId, ...) { }

    [HttpPost("organizers/{userId:long}/profiles/{profileId:long}/reject")]
    public Task<IActionResult> RejectOrganizer(long userId, long profileId,
        [FromBody] RejectProfileApprovalBffRequest request, ...) { }

    [HttpPost("venues/{userId:long}/profiles/{profileId:long}/approve")]
    public Task<IActionResult> ApproveVenue(long userId, long profileId, ...) { }

    [HttpPost("venues/{userId:long}/profiles/{profileId:long}/reject")]
    public Task<IActionResult> RejectVenue(long userId, long profileId,
        [FromBody] RejectProfileApprovalBffRequest request, ...) { }
}
```

Controller must:
- Extract `X-Aizen-User-Token` from request headers and pass to query/command
- Use the existing CQRS `ISender` pattern
- Use existing BFF response helper / envelope pattern
- Not accept or require browser Keycloak `Authorization` header

---

## STEP 10 — Auth and token model

Browser → BFF:
```http
X-Aizen-User-Token: Bearer <admin-identity-token>
```

BFF → Identity (constructed inside handlers):
```http
Authorization: Bearer <admin-panel-bff-keycloak-service-token>
X-Aizen-User-Token: Bearer <admin-identity-token>
```

**Do NOT forward the browser `Authorization` header.** The BFF acquires its own service token server-side via `IAdminPanelBffKeycloakServiceTokenProvider`.

If the Identity admin endpoints require a specific realm role (`Admin`) or service client role (`identity.admin`), follow the existing service-token role strategy. Document any authorization failures in the gap report.

---

## STEP 11 — Reject reason validation

Add BFF-side validation (follow the existing project's validation convention — FluentValidation or handler guard):

```
RejectProfileApprovalBffRequest.reason:
  - required
  - min length: 10
  - max length: 1000
  - strip leading/trailing whitespace before forwarding
```

Return validation error using the existing BFF error pattern if validation fails.

---

## STEP 12 — Logging

Add structured logs (do not log tokens or full rejection reason at Info level):

```csharp
_logger.LogInformation("Profile approval queue requested: status={Status} type={Type} page={Page}", ...);
_logger.LogInformation("Organizer profile approved: userId={UserId} profileId={ProfileId}", ...);
_logger.LogInformation("Venue profile rejected: userId={UserId} profileId={ProfileId}", ...);
_logger.LogWarning("Organizer list fetch failed from Identity: {Error}", ...);
_logger.LogDebug("Rejection reason length: {Length}", request.Reason.Length);
```

---

## STEP 13 — Register in DI

Add to `Program.cs` or the BFF's service registration:
- Any new services that are not auto-registered by the existing pattern
- The `RejectProfileIdentityRequest` request model needs no registration

---

## STEP 14 — Build and smoke tests

```bash
dotnet build Aizen.sln --no-incremental
```
Expected: **0 errors, 0 warnings about unused imports**

Smoke tests — replace `<BFF_PORT>` and `<admin_identity_token>`:

```bash
# ── QUEUE — pending (default) ─────────────────────────────────────────────────
curl -s "http://localhost:<BFF_PORT>/api/v1/admin-panel/users/profile-approvals?pageIndex=0&pageSize=20&status=pending" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>" | jq '{total: .approvals.count, pending: .summary.pendingOrganizers}'

# ── QUEUE — all types ─────────────────────────────────────────────────────────
curl -s "http://localhost:<BFF_PORT>/api/v1/admin-panel/users/profile-approvals?profileType=all&status=all&pageSize=50" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>" | jq '.approvals.count'

# ── QUEUE — search ────────────────────────────────────────────────────────────
curl -s "http://localhost:<BFF_PORT>/api/v1/admin-panel/users/profile-approvals?searchTerm=marina" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>" | jq '.approvals.items[].companyOrVenueName'

# ── ORGANIZER DETAIL ──────────────────────────────────────────────────────────
curl -s "http://localhost:<BFF_PORT>/api/v1/admin-panel/users/profile-approvals/organizers/<userId>/profiles/<profileId>" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>" | jq '{name: .organizer.applicant.fullName, status: .organizer.status}'

# ── VENUE DETAIL ──────────────────────────────────────────────────────────────
curl -s "http://localhost:<BFF_PORT>/api/v1/admin-panel/users/profile-approvals/venues/<userId>/profiles/<profileId>" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>" | jq '{name: .venue.venue.venueName, status: .venue.status}'

# ── APPROVE ORGANIZER ─────────────────────────────────────────────────────────
curl -s -X POST \
  "http://localhost:<BFF_PORT>/api/v1/admin-panel/users/profile-approvals/organizers/<userId>/profiles/<profileId>/approve" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>" | jq '.decision.status'
# Expected: "approved"

# ── IDEMPOTENCY — double approve must return 409 ──────────────────────────────
curl -s -o /dev/null -w "%{http_code}" -X POST \
  "http://localhost:<BFF_PORT>/api/v1/admin-panel/users/profile-approvals/organizers/<userId>/profiles/<profileId>/approve" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>"
# Expected: 409 (or the status Identity returns for already-processed profiles)

# ── REJECT VENUE ──────────────────────────────────────────────────────────────
curl -s -X POST \
  "http://localhost:<BFF_PORT>/api/v1/admin-panel/users/profile-approvals/venues/<userId>/profiles/<profileId>/reject" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>" \
  -H "Content-Type: application/json" \
  -d '{"reason": "Documents are incomplete and tax information could not be verified.", "reasonCategory": "missingDocuments", "notifyUser": true}' \
  | jq '.decision.status'
# Expected: "rejected"

# ── REJECT VALIDATION — short reason ─────────────────────────────────────────
curl -s -o /dev/null -w "%{http_code}" -X POST \
  "http://localhost:<BFF_PORT>/api/v1/admin-panel/users/profile-approvals/organizers/<userId>/profiles/<profileId2>/reject" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>" \
  -H "Content-Type: application/json" \
  -d '{"reason": "No"}'
# Expected: 400

# ── AUTH — no token → 401 ─────────────────────────────────────────────────────
curl -s -o /dev/null -w "%{http_code}" \
  "http://localhost:<BFF_PORT>/api/v1/admin-panel/users/profile-approvals?status=pending"
# Expected: 401

# ── AUTH — participant token → 403 ────────────────────────────────────────────
curl -s -o /dev/null -w "%{http_code}" \
  "http://localhost:<BFF_PORT>/api/v1/admin-panel/users/profile-approvals?status=pending" \
  -H "X-Aizen-User-Token: Bearer <participant_token>"
# Expected: 403

# ── NOT FOUND ─────────────────────────────────────────────────────────────────
curl -s -o /dev/null -w "%{http_code}" \
  "http://localhost:<BFF_PORT>/api/v1/admin-panel/users/profile-approvals/organizers/9999999/profiles/9999999" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>"
# Expected: 404
```

---

## STEP 15 — Generate final reports

Create these files in the BFF project's `docs/reports/` folder:

```
profile-approvals-bff-contract-audit-report.md
profile-approvals-identity-remote-call-report.md
profile-approvals-bff-endpoint-implementation-report.md
profile-approvals-auth-token-forwarding-report.md
profile-approvals-smoke-test-report.md
profile-approvals-final-gap-report.md
```

Each report must include:
- Files created / modified (with line counts)
- Exact Identity endpoints consumed
- Exact BFF endpoints exposed
- DTO field mapping (which Identity fields mapped, which were null-defaulted)
- Auth/token forwarding behavior
- List / detail data availability (fully real vs partially empty-state)
- Approve / reject test results (status codes)
- `reasonCategory`, `internalNote`, `notifyUser` support status in Identity
- Remaining gaps (unimplemented fields, missing Identity endpoints)
- Build result

---

## NON-NEGOTIABLE RULES

1. **Frontend calls BFF only.** Never call Identity directly from the React web.
2. **`long` IDs.** Never use `Guid` for `userId` / `profileId`.
3. **Preserve `AdminPanelAccess` policy** on all endpoints.
4. **BFF acquires Keycloak service token server-side** via `IAdminPanelBffKeycloakServiceTokenProvider`.
5. **Forward `X-Aizen-User-Token`** to every Identity call.
6. **Do NOT forward browser `Authorization`** header internally.
7. **Do not move Identity business logic into BFF.** BFF aggregates, maps, validates, normalizes only.
8. **Approve/reject must not return success if Identity command fails.**
9. **Reject `reason` is required.** Return 400 if missing or < 10 chars.
10. **Parallel fetch** for queue — do not serialize organizer + venue calls.
11. **No in-memory full-collection loads.** Prefer server-side filtering via Identity query params.
12. **Build must pass with 0 errors** before this task is considered complete.
13. **Read before writing.** Run STEP 0 and STEP 1 discovery, read existing controller and handler files, then write code.
