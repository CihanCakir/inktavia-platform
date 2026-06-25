# ADMIN_USERS_BFF_IMPLEMENTATION_PROMPT.md
# Copilot Agent Prompt — Admin Users BFF Endpoints

Paste this prompt into a Copilot Agent session opened in the **Aizen.AdminPanel.BFF** project.

---

You are implementing the **Admin Users** endpoints in `Aizen.AdminPanel.BFF`.
The React Admin Web's `/app/users` page calls two routes that currently return 404:

- `GET /api/v1/admin-panel/admin/users` — paginated user list with filters
- `GET /api/v1/admin-panel/admin/users/kpi` — 4 KPI metric counters

You will implement a BFF controller, a service that aggregates from the Identity module,
and all supporting DTOs. After implementing P0 endpoints, implement P1/P2 endpoints for the
user detail page.

---

## STEP 0 — Discovery (MANDATORY before writing any code)

```bash
# 1. Find existing BFF controllers to understand routing prefix and base class
find . -name "*BffController*" -o -name "*Controller*.cs" | grep -i bff | head -10

# 2. Find the Vessel BFF controller (already implemented) as the pattern to follow
find . -name "VesselBffController*" -o -path "*/Vessel/*Controller*" | head -5

# 3. Find Identity module service interfaces / query handlers
find . -path "*/Identity/*" -name "I*Service*" | head -20
find . -path "*/Identity/*" -name "*Query*" | head -20

# 4. Find ApplicationUser entity (Common identity submodule)
find . -name "ApplicationUser*" -o -name "IdentityUser*" | head -10

# 5. Understand existing route prefix
grep -r "RoutePrefix\|Route\(\"api" --include="*.cs" | grep -i "admin-panel\|bff" | head -10

# 6. Check how response DTOs are returned (direct Ok() vs Aizen envelope)
grep -r "return Ok\|AizenResponse\|BaseResponse" --include="*.cs" | head -10
```

Confirm:
- The controller base class and route attribute style
- Whether responses are wrapped in `{ header, body }` or returned directly via `Ok(dto)`
- How the Vessel BFF controller injects and calls module services

---

## STEP 1 — Create Response DTOs

Create `Aizen.AdminPanel.BFF/Users/Dtos/`:

```csharp
// AdminUserListItemBffDto.cs
namespace Aizen.AdminPanel.BFF.Users.Dtos;

public record AdminUserListItemBffDto(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Role,           // "SystemAdmin" | "Organizer" | "Participant" | "VenueManager"
    string Status,         // "Active" | "Pending" | "Suspended" | "Deactivated"
    string IdentityType,   // "Organizer" | "Participant" | "Venue"
    int VesselCount,
    string? LastLoginAt,   // ISO 8601 UTC string, nullable
    string CreatedAt,      // ISO 8601 UTC string
    string AvatarInitials
);

// AdminUserListResponseBffDto.cs
public record AdminUserListResponseBffDto(
    IReadOnlyList<AdminUserListItemBffDto> Items,
    int Total,
    int Page,
    int PageSize
);

// AdminUserKpiBffDto.cs
public record AdminUserKpiBffDto(
    int TotalUsers,
    int ActiveToday,
    int PendingVerification,
    int Suspended
);

// AdminUserDetailBffDto.cs
public record AdminUserDetailBffDto(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    bool EmailVerified,
    string? Phone,
    string Role,
    string Status,
    string IdentityType,
    string? OrganizationName,
    string? VenueName,
    string? Bio,
    AdminUserAddressDto? Address,
    string AvatarInitials,
    string KeycloakId,
    string? LastLoginAt,
    string CreatedAt,
    int VesselCount,
    int TotalTransactions,
    int TotalServiceRequests
);

public record AdminUserAddressDto(
    string? Street,
    string? City,
    string? Country
);

// AdminUserQuickBffDto.cs
public record AdminUserQuickBffDto(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Role,
    string Status,
    string IdentityType,
    string AvatarInitials,
    string KeycloakId,
    int VesselCount,
    IReadOnlyList<AdminUserVesselSummaryDto> Vessels,
    IReadOnlyList<AdminUserActivityItemDto> RecentActivity,
    string? LastLoginAt,
    string CreatedAt
);

public record AdminUserVesselSummaryDto(string Id, string Name, string Type);

public record AdminUserActivityItemDto(
    string Id,
    string Icon,
    string Label,
    string Timestamp,
    string RelativeTime
);

// AdminUserVesselRowBffDto.cs
public record AdminUserVesselRowBffDto(
    string Id,
    string Name,
    string Type,
    string FlagCountry,
    string RegistrationNo,
    string Status,
    string RegisteredAt
);

// AdminUserTransactionRowBffDto.cs
public record AdminUserTransactionRowBffDto(
    string Id,
    string Amount,
    string Type,   // "Credit" | "Debit"
    string Status, // "Cleared" | "Pending" | "Disputed"
    string Method,
    string Date
);

// AdminUserServiceRequestRowBffDto.cs
public record AdminUserServiceRequestRowBffDto(
    string Id,
    string Category,
    string Urgency,
    string Status,
    string? Provider,
    string CreatedAt
);

// AdminUserActivityEventBffDto.cs
public record AdminUserActivityEventBffDto(
    string Id,
    string Icon,
    string Label,
    string Timestamp,
    string RelativeTime
);
```

---

## STEP 2 — Create the Query Parameters model

```csharp
// AdminUserListQueryParams.cs
namespace Aizen.AdminPanel.BFF.Users;

public class AdminUserListQueryParams
{
    public string? Search { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }
    public string? IdentityType { get; set; }
    public string? RegisteredFrom { get; set; }   // YYYY-MM-DD
    public string? RegisteredTo { get; set; }      // YYYY-MM-DD
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    // Convert 1-based page to 0-based offset for DB queries
    public int Offset => (Page - 1) * PageSize;
}
```

---

## STEP 3 — Create AdminUserBffService

Create `Aizen.AdminPanel.BFF/Users/AdminUserBffService.cs`:

```csharp
namespace Aizen.AdminPanel.BFF.Users;

public class AdminUserBffService
{
    // Inject Identity module service interfaces or query dispatchers here.
    // Follow exactly the same pattern used in VesselBffService (if it exists) or
    // whatever module service injection pattern is already present in the project.
    //
    // Likely candidates to inject:
    //   IIdentityUserQueryService  (or separate IOrganizer/IParticipant/IVenue services)
    //   IVesselQueryService        (for vessel counts per user)
    //   IPaymentQueryService       (for transaction counts — optional for P2)
    //   IServiceRequestQueryService (for SR counts — optional for P2)

    // Read existing module service interfaces BEFORE implementing the constructor.

    // ── GetUserList ──────────────────────────────────────────────────────────
    public async Task<AdminUserListResponseBffDto> GetUserListAsync(
        AdminUserListQueryParams query,
        CancellationToken ct)
    {
        // 1. Query all identity profiles (Organizer + Participant + Venue) in parallel
        //    or query a unified AdminUserView if the Identity module exposes one.
        //
        // Apply filters:
        //   - search: WHERE LOWER(email) LIKE '%{search}%'
        //             OR LOWER(firstName) LIKE '%{search}%'
        //             OR LOWER(lastName) LIKE '%{search}%'
        //   - role: filter by assigned role
        //   - status: filter by ApplicationUser.Status
        //   - identityType: filter by which submodule owns the record
        //   - registeredFrom/registeredTo: filter by ApplicationUser.CreatedAt
        //
        // 2. Pagination: apply Offset + PageSize (1-based page → 0-based offset)
        //
        // 3. For each result, look up vesselCount from Vessel module.
        //    Use Task.WhenAll for parallel lookups if needed, or a single batch query.
        //
        // 4. Compute avatarInitials: $"{firstName[0]}{lastName[0]}".ToUpper()
        //
        // 5. Return AdminUserListResponseBffDto

        throw new NotImplementedException();
    }

    // ── GetKpi ───────────────────────────────────────────────────────────────
    public async Task<AdminUserKpiBffDto> GetKpiAsync(CancellationToken ct)
    {
        // Query ApplicationUser aggregate counts:
        //   totalUsers        = total count (all statuses)
        //   activeToday       = count WHERE LastLoginAt >= DateTime.UtcNow.Date
        //   pendingVerification = count WHERE Status == Pending
        //   suspended         = count WHERE Status == Suspended
        //
        // These can be 4 parallel scalar queries or a single GROUP BY Status query.
        // Use Task.WhenAll for parallel execution.

        throw new NotImplementedException();
    }

    // ── GetUserDetail ────────────────────────────────────────────────────────
    public async Task<AdminUserDetailBffDto?> GetUserDetailAsync(string id, CancellationToken ct)
    {
        // Parallel fetch:
        //   - Identity: get ApplicationUser + profile (Organizer or Participant or Venue)
        //   - Vessel: vesselCount
        //   - Payment: totalTransactions count (P2 — can return 0 for now)
        //   - ServiceRequest: totalServiceRequests count (P2 — can return 0 for now)
        //
        // Return null if user not found (controller will return 404).

        throw new NotImplementedException();
    }

    // ── GetUserQuick ─────────────────────────────────────────────────────────
    public async Task<AdminUserQuickBffDto?> GetUserQuickAsync(string id, CancellationToken ct)
    {
        // Parallel fetch:
        //   - Identity: ApplicationUser + profile
        //   - Vessel: top 3 vessels for this user
        //   - Activity: last 5 activity events (see GetUserActivity)
        //
        // recentActivity: derive from vessel registrations, transactions, service requests.
        // relativeTime: compute from event timestamp vs. UtcNow.
        //   Examples: "2 days ago", "Just now", "3 months ago"
        //   Use a simple helper: ComputeRelativeTime(DateTimeOffset eventTime)

        throw new NotImplementedException();
    }

    // ── GetUserVessels ───────────────────────────────────────────────────────
    public async Task<IReadOnlyList<AdminUserVesselRowBffDto>> GetUserVesselsAsync(
        string id, CancellationToken ct)
    {
        // Query Vessel module: list vessels WHERE OwnerId == id, ordered by RegisteredAt DESC

        throw new NotImplementedException();
    }

    // ── GetUserTransactions ──────────────────────────────────────────────────
    public async Task<IReadOnlyList<AdminUserTransactionRowBffDto>> GetUserTransactionsAsync(
        string id, CancellationToken ct)
    {
        // Query Payment module: list transactions WHERE UserId == id, ordered by Date DESC
        // Format amount as "{value} {currency}" string.

        throw new NotImplementedException();
    }

    // ── GetUserServiceRequests ───────────────────────────────────────────────
    public async Task<IReadOnlyList<AdminUserServiceRequestRowBffDto>> GetUserServiceRequestsAsync(
        string id, CancellationToken ct)
    {
        // Query ServiceRequest module: WHERE RequesterId == id, ordered by CreatedAt DESC

        throw new NotImplementedException();
    }

    // ── GetUserActivity ──────────────────────────────────────────────────────
    public async Task<IReadOnlyList<AdminUserActivityEventBffDto>> GetUserActivityAsync(
        string id, CancellationToken ct)
    {
        // Aggregate recent events from multiple sources in parallel:
        //   - Vessel registrations (icon: "directions_boat", label: "Vessel registered: {name}")
        //   - Transactions (icon: "receipt_long", label: "Transaction: {amount}")
        //   - Service requests (icon: "build", label: "Service request: {category}")
        //   - Status changes (icon: "manage_accounts", label: "Account status changed to {status}")
        //
        // Merge all, sort by timestamp DESC, return top 20.
        // Generate a stable Id per event (e.g., hash of type+entityId).
        // Compute relativeTime using ComputeRelativeTime helper.

        throw new NotImplementedException();
    }

    // ── Helper ───────────────────────────────────────────────────────────────
    private static string ComputeRelativeTime(DateTimeOffset eventTime)
    {
        var diff = DateTimeOffset.UtcNow - eventTime;
        if (diff.TotalMinutes < 1) return "Just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} minutes ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hours ago";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} days ago";
        if (diff.TotalDays < 30) return $"{(int)(diff.TotalDays / 7)} weeks ago";
        if (diff.TotalDays < 365) return $"{(int)(diff.TotalDays / 30)} months ago";
        return $"{(int)(diff.TotalDays / 365)} years ago";
    }

    private static string ComputeAvatarInitials(string? firstName, string? lastName)
    {
        var f = firstName?.Length > 0 ? firstName[0].ToString().ToUpperInvariant() : "";
        var l = lastName?.Length > 0 ? lastName[0].ToString().ToUpperInvariant() : "";
        return $"{f}{l}";
    }
}
```

---

## STEP 4 — Create AdminUserBffController

Create `Aizen.AdminPanel.BFF/Users/AdminUserBffController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Aizen.AdminPanel.BFF.Users.Dtos;

namespace Aizen.AdminPanel.BFF.Users;

// IMPORTANT: Read the existing BFF controller base class and route attribute style
// from other controllers in this project before writing this class.
// Use the EXACT same pattern — base class, [Authorize] attributes, route prefix.

[ApiController]
[Route("admin/users")]   // Adjust if the BFF uses a different route prefix convention
public class AdminUserBffController : ControllerBase  // Replace with actual BFF base class
{
    private readonly AdminUserBffService _service;

    public AdminUserBffController(AdminUserBffService service)
    {
        _service = service;
    }

    // P0 — User list
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] AdminUserListQueryParams query,
        CancellationToken ct)
    {
        var result = await _service.GetUserListAsync(query, ct);
        return Ok(result);
    }

    // P0 — KPI
    [HttpGet("kpi")]
    public async Task<IActionResult> GetKpi(CancellationToken ct)
    {
        var result = await _service.GetKpiAsync(ct);
        return Ok(result);
    }

    // P1 — User detail
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserDetail(string id, CancellationToken ct)
    {
        var result = await _service.GetUserDetailAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // P1 — Quick panel data
    [HttpGet("{id}/quick")]
    public async Task<IActionResult> GetUserQuick(string id, CancellationToken ct)
    {
        var result = await _service.GetUserQuickAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // P2 — Vessels tab
    [HttpGet("{id}/vessels")]
    public async Task<IActionResult> GetUserVessels(string id, CancellationToken ct)
    {
        var result = await _service.GetUserVesselsAsync(id, ct);
        return Ok(result);
    }

    // P2 — Transactions tab
    [HttpGet("{id}/transactions")]
    public async Task<IActionResult> GetUserTransactions(string id, CancellationToken ct)
    {
        var result = await _service.GetUserTransactionsAsync(id, ct);
        return Ok(result);
    }

    // P2 — Service requests tab
    [HttpGet("{id}/service-requests")]
    public async Task<IActionResult> GetUserServiceRequests(string id, CancellationToken ct)
    {
        var result = await _service.GetUserServiceRequestsAsync(id, ct);
        return Ok(result);
    }

    // P2 — Activity timeline
    [HttpGet("{id}/activity")]
    public async Task<IActionResult> GetUserActivity(string id, CancellationToken ct)
    {
        var result = await _service.GetUserActivityAsync(id, ct);
        return Ok(result);
    }
}
```

---

## STEP 5 — Register the service in DI

In the BFF's `Program.cs` or `Startup.cs` (follow existing registration pattern):

```csharp
builder.Services.AddScoped<AdminUserBffService>();
```

---

## STEP 6 — Implement AdminUserBffService (replace NotImplementedException stubs)

Go back to `AdminUserBffService.cs` and implement each method.

**Implementation order:**
1. `GetKpiAsync` — simplest: 4 scalar counts from a single table
2. `GetUserListAsync` — filtered + paginated query with vessel count join
3. `GetUserDetailAsync` — parallel aggregation of profile + counts
4. `GetUserQuickAsync` — parallel fetch: profile + vessels + recent activity
5. `GetUserVesselsAsync` — simple vessel list query
6. `GetUserTransactionsAsync` — simple payment list query
7. `GetUserServiceRequestsAsync` — simple SR list query
8. `GetUserActivityAsync` — multi-source merge + sort

**Key implementation notes:**
- The Identity module may expose a unified `ApplicationUser` table or keep users across
  Organizer/Participant/Venue sub-tables. Read the actual entities before writing query logic.
- For `GetUserListAsync`, do NOT load all users into memory and filter in C#.
  Push filtering to the database using EF Core `IQueryable` + `Where` chains.
- For `vesselCount` in the list, use a single batch query (e.g., `GROUP BY OwnerId`)
  rather than N individual queries per user.
- If `LastLoginAt` is stored in Keycloak and not in the local DB, return `null` for now
  and add a TODO comment for Keycloak event sync.
- DateTime fields must be stored and returned as UTC. Use `DateTimeOffset.UtcNow` not `DateTime.Now`.
- `RegisteredFrom` / `RegisteredTo` are date-only strings (YYYY-MM-DD). Parse as:
  ```csharp
  DateOnly.TryParse(query.RegisteredFrom, out var fromDate)
  // compare: user.CreatedAt.Date >= fromDate.ToDateTime(TimeOnly.MinValue)
  ```

---

## STEP 7 — Smoke test

After the BFF is running at `localhost:17001`, verify with curl:

```bash
# P0 — user list
curl -s "http://localhost:17001/api/v1/admin-panel/admin/users?page=1&pageSize=20" \
  -H "Authorization: Bearer <keycloak_token>" \
  -H "X-Aizen-User-Token: Bearer <identity_token>" | jq .

# P0 — KPI
curl -s "http://localhost:17001/api/v1/admin-panel/admin/users/kpi" \
  -H "Authorization: Bearer <keycloak_token>" \
  -H "X-Aizen-User-Token: Bearer <identity_token>" | jq .

# P0 — search filter
curl -s "http://localhost:17001/api/v1/admin-panel/admin/users?search=ahmet&page=1&pageSize=10" \
  -H "Authorization: Bearer <keycloak_token>" \
  -H "X-Aizen-User-Token: Bearer <identity_token>" | jq .

# P0 — role + status filter
curl -s "http://localhost:17001/api/v1/admin-panel/admin/users?role=Organizer&status=Active&page=1&pageSize=10" \
  -H "Authorization: Bearer <keycloak_token>" \
  -H "X-Aizen-User-Token: Bearer <identity_token>" | jq .

# P1 — user detail (replace with a real user ID)
curl -s "http://localhost:17001/api/v1/admin-panel/admin/users/usr_9f3b2a" \
  -H "Authorization: Bearer <keycloak_token>" \
  -H "X-Aizen-User-Token: Bearer <identity_token>" | jq .
```

**Expected results:**
- `/admin/users` → 200 with `{ items: [...], total: N, page: 1, pageSize: 20 }`
- `/admin/users/kpi` → 200 with `{ totalUsers, activeToday, pendingVerification, suspended }`
- No 404 responses for either endpoint
- `items[].avatarInitials` is 2 uppercase chars
- `items[].vesselCount` is an integer ≥ 0 (not null)

---

## NON-NEGOTIABLE RULES

1. **Read existing code before writing.** Find and follow the exact controller base class,
   route prefix convention, and module service injection pattern already in this BFF project.

2. **Do not call Identity module HTTP endpoints.** Call the Identity module via in-process
   service interfaces or MediatR queries — NOT via HTTP. This is a modular monolith.

3. **Push all filtering to the database.** No in-memory filtering over full user lists.

4. **`page` is 1-based in the API.** Convert to 0-based offset for DB queries: `(page - 1) * pageSize`.

5. **All DateTime fields must be UTC.** Use `DateTimeOffset` or `DateTime` with UTC kind.
   PostgreSQL stores UTC — do not apply local timezone conversions.

6. **Return types must exactly match the frontend DTO shapes.**
   The frontend deserializes JSON directly into the TypeScript types in `user.types.ts`.
   Field names must be camelCase in JSON (use `[JsonPropertyName]` or camelCase serialization).

7. **Verify JSON serialization uses camelCase.** If the project default is PascalCase,
   add `[JsonPropertyName("items")]` to DTO properties or configure global camelCase serializer.

8. **Implement P0 endpoints first.** Get the list + KPI working before moving to P1/P2.

9. **Zero placeholder data.** Do not return hardcoded mock data. Every field must come from
   actual database queries.
