# ADMIN_USERS_BFF_API_CONTRACT.md
# Admin Users — BFF API Contract

## Context

The `/app/users` page in the React Admin Web calls two BFF endpoints that currently return 404.
This document defines the exact request/response contract the frontend expects for all
`/admin/users/*` routes discovered in `src/shared/api/hooks/useUsers.ts`.

BFF base: `Aizen.AdminPanel.BFF`  
Controller route prefix: `admin/users`  
Full public path: `GET /api/v1/admin-panel/admin/users`

---

## Data Sources (Identity Module)

The BFF aggregates from three Identity submodules:

| Identity submodule | Entity | Role |
|--------------------|--------|------|
| `Aizen.Modules.Identity.Organizer` | `OrganizerProfile` | `Organizer` |
| `Aizen.Modules.Identity.Participant` | `ParticipantProfile` | `Participant` |
| `Aizen.Modules.Identity.Venue` | `VenueProfile` | `VenueManager` |
| `Aizen.Modules.Identity.Common` | `ApplicationUser` | All — Keycloak link, status, lastLoginAt |
| `Aizen.Modules.Vessel` | `Vessel` | vessel count per user |

---

## Endpoint 1 — Paginated User List

```
GET /api/v1/admin-panel/admin/users
```

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `search` | string | No | Free-text search against firstName, lastName, email |
| `role` | string | No | `SystemAdmin` \| `Organizer` \| `Participant` \| `VenueManager` \| `""` |
| `status` | string | No | `Active` \| `Pending` \| `Suspended` \| `Deactivated` \| `""` |
| `identityType` | string | No | `Organizer` \| `Participant` \| `Venue` \| `""` |
| `registeredFrom` | string (ISO date) | No | Filter by createdAt ≥ date (format: `YYYY-MM-DD`) |
| `registeredTo` | string (ISO date) | No | Filter by createdAt ≤ date (format: `YYYY-MM-DD`) |
| `page` | int | No | 1-based page number (default: 1) |
| `pageSize` | int | No | Items per page (default: 20, max: 100) |

### Response — 200 OK

The frontend calls `httpClient.get<UserListResponse>(...)` and reads `r.data` directly.
The BFF must return the response body **unwrapped** (no Aizen envelope) OR the httpClient
response interceptor unwraps it. Verify which `responseNormalizer` / interceptor is active.

**If envelope IS used (`header` + `body`):**
```json
{
  "header": { "isSuccess": true, "errorCode": 0 },
  "body": {
    "items": [ ... ],
    "total": 142,
    "page": 1,
    "pageSize": 20
  }
}
```

**If NO envelope (direct response — check existing httpClient interceptor):**
```json
{
  "items": [ ... ],
  "total": 142,
  "page": 1,
  "pageSize": 20
}
```

### `UserListItemDto` shape (per item in `items[]`)

```json
{
  "id": "usr_9f3b2a",
  "firstName": "Ahmet",
  "lastName": "Yılmaz",
  "email": "ahmet@example.com",
  "phone": "+905321234567",
  "role": "Organizer",
  "status": "Active",
  "identityType": "Organizer",
  "vesselCount": 3,
  "lastLoginAt": "2026-06-20T14:32:00Z",
  "createdAt": "2024-11-10T09:15:00Z",
  "avatarInitials": "AY"
}
```

### Field Notes

| Field | Source | Notes |
|-------|--------|-------|
| `id` | `ApplicationUser.Id` or `KeycloakId` | String ID consistent with Identity module |
| `firstName` | `OrganizerProfile.FirstName` / `ParticipantProfile.FirstName` / `VenueProfile.ContactName` | Varies per identity type |
| `lastName` | Same as above | |
| `email` | `ApplicationUser.Email` | From Keycloak/common identity |
| `phone` | Profile phone field | Optional |
| `role` | Derived from which submodule record exists | `Organizer` if OrganizerProfile exists, etc. |
| `status` | `ApplicationUser.Status` enum → string | Map enum → `Active/Pending/Suspended/Deactivated` |
| `identityType` | `Organizer` \| `Participant` \| `Venue` | Determined by which submodule owns this user |
| `vesselCount` | `Vessel.Count(v => v.OwnerId == userId)` | Join to Vessel module |
| `lastLoginAt` | `ApplicationUser.LastLoginAt` or Keycloak event | Nullable ISO string |
| `createdAt` | `ApplicationUser.CreatedAt` | ISO string UTC |
| `avatarInitials` | BFF computed: `$"{firstName[0]}{lastName[0]}"`.ToUpper() | Max 2 chars |

### Role/Status Enum Mappings (C# → string)

```csharp
// Role
public enum AdminUserRole { SystemAdmin, Organizer, Participant, VenueManager }

// Status
public enum AdminUserStatus { Active, Pending, Suspended, Deactivated }

// IdentityType
public enum AdminIdentityType { Organizer, Participant, Venue }
```

---

## Endpoint 2 — KPI Metrics

```
GET /api/v1/admin-panel/admin/users/kpi
```

No query parameters.

### Response — 200 OK

```json
{
  "totalUsers": 1420,
  "activeToday": 87,
  "pendingVerification": 12,
  "suspended": 5
}
```

### Field Notes

| Field | Source / Logic |
|-------|----------------|
| `totalUsers` | `COUNT(ApplicationUser)` — all users in the system |
| `activeToday` | `COUNT(ApplicationUser WHERE LastLoginAt >= UTC_TODAY)` |
| `pendingVerification` | `COUNT(ApplicationUser WHERE Status == Pending)` |
| `suspended` | `COUNT(ApplicationUser WHERE Status == Suspended)` |

---

## Additional Endpoints (also needed — same controller)

These endpoints are wired in `useUsers.ts` and will 404 once the user list is fixed.

### GET /admin/users/:id
Returns `UserDetailDto`. See `user.types.ts` for full shape.

```json
{
  "id": "usr_9f3b2a",
  "firstName": "Ahmet",
  "lastName": "Yılmaz",
  "email": "ahmet@example.com",
  "emailVerified": true,
  "phone": "+905321234567",
  "role": "Organizer",
  "status": "Active",
  "identityType": "Organizer",
  "organizationName": "Blue Marina Ltd.",
  "venueName": null,
  "bio": "...",
  "address": { "street": "Atatürk Cad.", "city": "Bodrum", "country": "Turkey" },
  "avatarInitials": "AY",
  "keycloakId": "kc-uuid-here",
  "lastLoginAt": "2026-06-20T14:32:00Z",
  "createdAt": "2024-11-10T09:15:00Z",
  "vesselCount": 3,
  "totalTransactions": 24,
  "totalServiceRequests": 7
}
```

### GET /admin/users/:id/quick
Lightweight panel data for the slide panel. Returns `UserQuickDto`.

```json
{
  "id": "usr_9f3b2a",
  "firstName": "Ahmet",
  "lastName": "Yılmaz",
  "email": "ahmet@example.com",
  "phone": "+905321234567",
  "role": "Organizer",
  "status": "Active",
  "identityType": "Organizer",
  "avatarInitials": "AY",
  "keycloakId": "kc-uuid-here",
  "vesselCount": 3,
  "vessels": [
    { "id": "v_123", "name": "Serenity Horizon", "type": "Yacht" }
  ],
  "recentActivity": [
    { "icon": "directions_boat", "label": "Vessel registered: Serenity Horizon", "relativeTime": "2 days ago" }
  ],
  "lastLoginAt": "2026-06-20T14:32:00Z",
  "createdAt": "2024-11-10T09:15:00Z"
}
```

### GET /admin/users/:id/vessels
Returns `UserVesselRowDto[]`.

```json
[
  {
    "id": "v_123",
    "name": "Serenity Horizon",
    "type": "Yacht",
    "flagCountry": "Turkey",
    "registrationNo": "TR-BOD-2024-001",
    "status": "Active",
    "registeredAt": "2024-12-01T00:00:00Z"
  }
]
```

### GET /admin/users/:id/transactions
Returns `UserTransactionRowDto[]`.

```json
[
  {
    "id": "txn_456",
    "amount": "2500.00 USD",
    "type": "Credit",
    "status": "Cleared",
    "method": "Bank Transfer",
    "date": "2026-05-15T10:22:00Z"
  }
]
```

### GET /admin/users/:id/service-requests
Returns `UserServiceRequestRowDto[]`.

```json
[
  {
    "id": "sr_789",
    "category": "Engine Repair",
    "urgency": "High",
    "status": "InProgress",
    "provider": "Marina Tech Services",
    "createdAt": "2026-06-10T08:00:00Z"
  }
]
```

### GET /admin/users/:id/activity
Returns `UserActivityEventDto[]` — recent platform activity log.

```json
[
  {
    "id": "act_001",
    "icon": "directions_boat",
    "label": "Vessel registered: Serenity Horizon",
    "timestamp": "2026-06-20T09:15:00Z",
    "relativeTime": "2 days ago"
  },
  {
    "id": "act_002",
    "icon": "receipt_long",
    "label": "Transaction completed: 2500 USD",
    "timestamp": "2026-06-18T14:30:00Z",
    "relativeTime": "4 days ago"
  }
]
```

---

## Response Format Decision

Before implementation, confirm with frontend engineer which format `httpClient` returns.

**Check `src/shared/api/httpClient.ts`:**
```typescript
// Option A — response interceptor unwraps envelope:
// instance.interceptors.response.use(r => r.data.body ?? r.data)
// → BFF must use standard Aizen envelope { header, body }

// Option B — no interceptor, raw response:
// .then((r) => r.data)  in useUsers.ts already reads r.data
// → BFF must return the DTO directly as the response body
```

The `useUsers.ts` hooks use `.then((r) => r.data)` — this means the shape above (direct, no envelope)
is what the BFF controller's `return Ok(dto)` should produce.

---

## Priority

| Endpoint | Priority | Blocks |
|----------|----------|--------|
| `GET /admin/users` | **P0** | User list page renders nothing |
| `GET /admin/users/kpi` | **P0** | KPI cards show blank |
| `GET /admin/users/:id` | P1 | User detail page |
| `GET /admin/users/:id/quick` | P1 | Slide panel |
| `GET /admin/users/:id/vessels` | P2 | Detail tab |
| `GET /admin/users/:id/transactions` | P2 | Detail tab |
| `GET /admin/users/:id/service-requests` | P2 | Detail tab |
| `GET /admin/users/:id/activity` | P2 | Detail tab |
