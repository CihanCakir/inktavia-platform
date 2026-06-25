# ADMIN_USERS_ACTIVITY_BFF_PROMPT.md
# User Activity Feed — BFF Implementation Prompt

Paste this entire file into a Copilot Agent session opened in the `Aizen.AdminPanel.BFF` project.

---

## Context

The React Admin Web's user detail page has an **Activity** tab that calls:

```
GET /api/v1/admin-panel/admin/users/{id}/activity
```

The current implementation returns a simple flat list. This prompt upgrades it to a **paginated,
filterable activity feed** that aggregates events from four backend modules:

| Module | Events |
|--------|--------|
| `Aizen.Modules.Identity.Common` | Login, status changes, email verification, profile updates |
| `Aizen.Modules.Vessel` | Vessel registered, status changed, document uploaded |
| `Aizen.Modules.Payment` | Transaction created, transaction status changed |
| `Aizen.Modules.ServiceRequest` | Service request created, status changed |

---

## STEP 0 — Read existing code (MANDATORY)

```bash
# Read the existing BFF activity endpoint if it exists:
cat Aizen.AdminPanel.BFF/Controllers/AdminUserBffController.cs
cat Aizen.AdminPanel.BFF/Services/AdminUserBffService.cs
cat Aizen.AdminPanel.BFF/DTOs/UserActivityEventDto.cs   # may not exist yet

# Read relevant domain entities for each module:
grep -r "LastLoginAt\|LoginHistory\|AuditLog" Aizen.Modules.Identity.Common --include="*.cs" -l
grep -r "VesselStatusHistory\|VesselDocument" Aizen.Modules.Vessel --include="*.cs" -l
grep -r "Transaction\|PaymentTransaction" Aizen.Modules.Payment --include="*.cs" -l
grep -r "ServiceRequest" Aizen.Modules.ServiceRequest --include="*.cs" -l
```

Confirm before writing:
- Which `DbContext` each module uses
- How `IAdminUserBffService` is structured
- Whether an activity/audit log table already exists
- How other list endpoints return pagination (existing pattern to follow)

---

## STEP 1 — DTOs

Create `Aizen.AdminPanel.BFF/DTOs/UserActivityDtos.cs`:

```csharp
namespace Aizen.AdminPanel.BFF.DTOs;

// ─── Enums ────────────────────────────────────────────────────────────────────

public enum ActivityCategory
{
    Vessel,
    Transaction,
    Service,
    Identity,
    System
}

public enum ActivityEventType
{
    VesselRegistered,
    VesselStatusChanged,
    VesselDocumentUploaded,
    TransactionCreated,
    TransactionStatusChanged,
    ServiceRequestCreated,
    ServiceRequestStatusChanged,
    ProfileUpdated,
    Login,
    StatusChanged,
    EmailVerified,
    SystemNote
}

// ─── Event DTO ────────────────────────────────────────────────────────────────

public record UserActivityEventDto
{
    public required string Id { get; init; }
    public required string Type { get; init; }        // ActivityEventType as camelCase string
    public required string Category { get; init; }    // ActivityCategory as camelCase string
    public required string Icon { get; init; }        // Material Symbol name
    public required string Label { get; init; }       // Human-readable label (Turkish)
    public string? Description { get; init; }         // Optional longer description
    public required DateTimeOffset Timestamp { get; init; }
    public required string RelativeTime { get; init; } // "2 saat önce", "Dün", etc.
    public ActivityEventMetaDto? Meta { get; init; }
}

public record ActivityEventMetaDto
{
    public string? EntityId { get; init; }
    public string? EntityName { get; init; }
    public string? PreviousValue { get; init; }
    public string? NewValue { get; init; }
}

// ─── Query params ─────────────────────────────────────────────────────────────

public record UserActivityQueryParams
{
    public string? Category { get; init; }   // vessel | transaction | service | identity | system | null = all
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

// ─── Response ─────────────────────────────────────────────────────────────────

public record UserActivityResponseDto
{
    public required IReadOnlyList<UserActivityEventDto> Items { get; init; }
    public required int Total { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public IReadOnlyList<string>? Warnings { get; init; }
}
```

---

## STEP 2 — Activity aggregation service

Add the following method to `IAdminUserBffService` and implement it in `AdminUserBffService`:

```csharp
Task<UserActivityResponseDto> GetUserActivityAsync(
    string userId,
    UserActivityQueryParams queryParams,
    CancellationToken cancellationToken = default);
```

### Implementation strategy

Collect raw activity records from each module's database, merge them into a single list,
sort by timestamp descending, then apply pagination. Do NOT make HTTP calls — use in-process
service/repository calls to each module.

```csharp
public async Task<UserActivityResponseDto> GetUserActivityAsync(
    string userId,
    UserActivityQueryParams q,
    CancellationToken ct = default)
{
    var events = new List<UserActivityEventDto>();
    var warnings = new List<string>();

    // ── 1. Identity events ────────────────────────────────────────────────────
    // Only include identity events when category is null (all) or "identity"
    if (string.IsNullOrEmpty(q.Category) || q.Category == "identity")
    {
        try
        {
            var identityEvents = await CollectIdentityEventsAsync(userId, q, ct);
            events.AddRange(identityEvents);
        }
        catch (Exception ex)
        {
            warnings.Add($"Identity events unavailable: {ex.Message}");
        }
    }

    // ── 2. Vessel events ──────────────────────────────────────────────────────
    if (string.IsNullOrEmpty(q.Category) || q.Category == "vessel")
    {
        try
        {
            var vesselEvents = await CollectVesselEventsAsync(userId, q, ct);
            events.AddRange(vesselEvents);
        }
        catch (Exception ex)
        {
            warnings.Add($"Vessel events unavailable: {ex.Message}");
        }
    }

    // ── 3. Transaction events ─────────────────────────────────────────────────
    if (string.IsNullOrEmpty(q.Category) || q.Category == "transaction")
    {
        try
        {
            var txEvents = await CollectTransactionEventsAsync(userId, q, ct);
            events.AddRange(txEvents);
        }
        catch (Exception ex)
        {
            warnings.Add($"Transaction events unavailable: {ex.Message}");
        }
    }

    // ── 4. Service request events ─────────────────────────────────────────────
    if (string.IsNullOrEmpty(q.Category) || q.Category == "service")
    {
        try
        {
            var serviceEvents = await CollectServiceRequestEventsAsync(userId, q, ct);
            events.AddRange(serviceEvents);
        }
        catch (Exception ex)
        {
            warnings.Add($"Service events unavailable: {ex.Message}");
        }
    }

    // ── Sort + paginate ───────────────────────────────────────────────────────
    var sorted = events
        .OrderByDescending(e => e.Timestamp)
        .ToList();

    var total = sorted.Count;
    var items = sorted
        .Skip((q.Page - 1) * q.PageSize)
        .Take(q.PageSize)
        .ToList();

    return new UserActivityResponseDto
    {
        Items = items,
        Total = total,
        Page = q.Page,
        PageSize = q.PageSize,
        Warnings = warnings.Count > 0 ? warnings : null,
    };
}
```

### Private collection helpers

Implement each helper following the same pattern. Below are stubs — fill in the actual EF Core queries
based on the entity classes you discover in STEP 0.

```csharp
// ─── Identity events ──────────────────────────────────────────────────────────

private async Task<IEnumerable<UserActivityEventDto>> CollectIdentityEventsAsync(
    string userId, UserActivityQueryParams q, CancellationToken ct)
{
    var result = new List<UserActivityEventDto>();
    var now = DateTimeOffset.UtcNow;

    // Last login — from ApplicationUser.LastLoginAt
    // var user = await _identityDb.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
    // if (user?.LastLoginAt != null)
    //     result.Add(BuildEvent("login_" + userId, ActivityEventType.Login, ActivityCategory.Identity,
    //         "schedule", "Sisteme giriş yapıldı", null, user.LastLoginAt.Value, now));

    // Status changes — from UserStatusHistory if it exists, else skip
    // Profile updates — from audit log if it exists, else skip

    // Apply date filter
    return ApplyDateFilter(result, q);
}

// ─── Vessel events ────────────────────────────────────────────────────────────

private async Task<IEnumerable<UserActivityEventDto>> CollectVesselEventsAsync(
    string userId, UserActivityQueryParams q, CancellationToken ct)
{
    var result = new List<UserActivityEventDto>();
    var now = DateTimeOffset.UtcNow;

    // Vessel registrations
    // var vessels = await _vesselDb.Vessels
    //     .Where(v => v.OwnerId == userId)
    //     .Select(v => new { v.Id, v.Name, v.CreatedAt })
    //     .ToListAsync(ct);
    // foreach (var v in vessels)
    //     result.Add(BuildEvent("vessel_reg_" + v.Id, ActivityEventType.VesselRegistered,
    //         ActivityCategory.Vessel, "directions_boat",
    //         $"Gemi kaydedildi: {v.Name}", null, v.CreatedAt, now,
    //         new ActivityEventMetaDto { EntityId = v.Id.ToString(), EntityName = v.Name }));

    // Vessel status history — if VesselStatusHistory table exists
    // var statusHistory = await _vesselDb.VesselStatusHistories
    //     .Where(h => h.Vessel.OwnerId == userId)
    //     .Include(h => h.Vessel)
    //     .OrderByDescending(h => h.ChangedAt)
    //     .ToListAsync(ct);
    // foreach (var h in statusHistory)
    //     result.Add(BuildEvent("vessel_status_" + h.Id, ActivityEventType.VesselStatusChanged, ...));

    return ApplyDateFilter(result, q);
}

// ─── Transaction events ───────────────────────────────────────────────────────

private async Task<IEnumerable<UserActivityEventDto>> CollectTransactionEventsAsync(
    string userId, UserActivityQueryParams q, CancellationToken ct)
{
    var result = new List<UserActivityEventDto>();
    var now = DateTimeOffset.UtcNow;

    // var txs = await _paymentDb.Transactions
    //     .Where(t => t.UserId == userId)
    //     .OrderByDescending(t => t.CreatedAt)
    //     .Take(100) // hard cap — paginate later from sorted merge
    //     .ToListAsync(ct);
    // foreach (var tx in txs)
    //     result.Add(BuildEvent("tx_" + tx.Id, ActivityEventType.TransactionCreated,
    //         ActivityCategory.Transaction, "payments",
    //         $"İşlem oluşturuldu: {tx.Amount} {tx.Currency}", null, tx.CreatedAt, now,
    //         new ActivityEventMetaDto { EntityId = tx.Id.ToString() }));

    return ApplyDateFilter(result, q);
}

// ─── Service request events ───────────────────────────────────────────────────

private async Task<IEnumerable<UserActivityEventDto>> CollectServiceRequestEventsAsync(
    string userId, UserActivityQueryParams q, CancellationToken ct)
{
    var result = new List<UserActivityEventDto>();
    var now = DateTimeOffset.UtcNow;

    // var requests = await _serviceDb.ServiceRequests
    //     .Where(r => r.UserId == userId)
    //     .OrderByDescending(r => r.CreatedAt)
    //     .Take(100)
    //     .ToListAsync(ct);
    // foreach (var r in requests)
    //     result.Add(BuildEvent("sr_" + r.Id, ActivityEventType.ServiceRequestCreated,
    //         ActivityCategory.Service, "engineering",
    //         $"Servis talebi oluşturuldu: {r.Category}", null, r.CreatedAt, now,
    //         new ActivityEventMetaDto { EntityId = r.Id.ToString(), EntityName = r.Category }));

    return ApplyDateFilter(result, q);
}
```

### Shared builder helpers

Add these private methods to `AdminUserBffService`:

```csharp
private static UserActivityEventDto BuildEvent(
    string id,
    ActivityEventType type,
    ActivityCategory category,
    string icon,
    string label,
    string? description,
    DateTimeOffset timestamp,
    DateTimeOffset now,
    ActivityEventMetaDto? meta = null)
{
    return new UserActivityEventDto
    {
        Id = id,
        Type = ToEventTypeString(type),
        Category = ToCategoryString(category),
        Icon = icon,
        Label = label,
        Description = description,
        Timestamp = timestamp,
        RelativeTime = FormatRelativeTime(timestamp, now),
        Meta = meta,
    };
}

private static string ToEventTypeString(ActivityEventType type) => type switch
{
    ActivityEventType.VesselRegistered => "vessel_registered",
    ActivityEventType.VesselStatusChanged => "vessel_status_changed",
    ActivityEventType.VesselDocumentUploaded => "vessel_document_uploaded",
    ActivityEventType.TransactionCreated => "transaction_created",
    ActivityEventType.TransactionStatusChanged => "transaction_status_changed",
    ActivityEventType.ServiceRequestCreated => "service_request_created",
    ActivityEventType.ServiceRequestStatusChanged => "service_request_status_changed",
    ActivityEventType.ProfileUpdated => "profile_updated",
    ActivityEventType.Login => "login",
    ActivityEventType.StatusChanged => "status_changed",
    ActivityEventType.EmailVerified => "email_verified",
    ActivityEventType.SystemNote => "system_note",
    _ => "unknown"
};

private static string ToCategoryString(ActivityCategory cat) => cat switch
{
    ActivityCategory.Vessel => "vessel",
    ActivityCategory.Transaction => "transaction",
    ActivityCategory.Service => "service",
    ActivityCategory.Identity => "identity",
    ActivityCategory.System => "system",
    _ => "system"
};

/// <summary>
/// Returns Turkish relative time string: "Az önce", "5 dakika önce", "2 saat önce",
/// "Dün", "X Haziran 2026", etc.
/// </summary>
private static string FormatRelativeTime(DateTimeOffset timestamp, DateTimeOffset now)
{
    var diff = now - timestamp;
    if (diff.TotalMinutes < 1) return "Az önce";
    if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} dakika önce";
    if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} saat önce";
    if (diff.TotalDays < 2) return "Dün";
    if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} gün önce";
    return timestamp.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
}

private static IEnumerable<UserActivityEventDto> ApplyDateFilter(
    IEnumerable<UserActivityEventDto> events,
    UserActivityQueryParams q)
{
    if (q.DateFrom.HasValue)
        events = events.Where(e => e.Timestamp.Date >= q.DateFrom.Value.ToDateTime(TimeOnly.MinValue));
    if (q.DateTo.HasValue)
        events = events.Where(e => e.Timestamp.Date <= q.DateTo.Value.ToDateTime(TimeOnly.MaxValue));
    return events;
}
```

---

## STEP 3 — Controller endpoint

In `AdminUserBffController`, add or update the activity endpoint:

```csharp
/// <summary>
/// Paginated, filterable activity feed for a user.
/// Aggregates events from Identity, Vessel, Payment, and ServiceRequest modules.
/// </summary>
[HttpGet("{id}/activity")]
public async Task<IActionResult> GetUserActivity(
    [FromRoute] string id,
    [FromQuery] UserActivityQueryParams queryParams,
    CancellationToken cancellationToken)
{
    var result = await _adminUserBffService.GetUserActivityAsync(id, queryParams, cancellationToken);

    return Ok(new
    {
        header = new { isSuccess = true, errorCode = 0 },
        body = result,
    });
}
```

**Important:** The response `body` IS `UserActivityResponseDto` directly (not nested under another key).
The frontend reads `envelope.body.items`, `envelope.body.total`, `envelope.body.page`, `envelope.body.pageSize`.

---

## STEP 4 — Validation

Add a FluentValidation validator for `UserActivityQueryParams`:

```csharp
public class UserActivityQueryParamsValidator : AbstractValidator<UserActivityQueryParams>
{
    private static readonly string[] ValidCategories =
        ["vessel", "transaction", "service", "identity", "system"];

    public UserActivityQueryParamsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Category)
            .Must(c => string.IsNullOrEmpty(c) || ValidCategories.Contains(c))
            .WithMessage("Invalid category. Valid: vessel, transaction, service, identity, system.");
        RuleFor(x => x.DateFrom)
            .LessThanOrEqualTo(x => x.DateTo)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue)
            .WithMessage("dateFrom must be before dateTo.");
    }
}
```

Register in DI alongside other validators.

---

## STEP 5 — Implementation order

1. Create `UserActivityDtos.cs` (STEP 1)
2. Add builder helpers to `AdminUserBffService` (STEP 2 — helpers section)
3. Implement `CollectIdentityEventsAsync` — start with `LastLoginAt` only (simplest first)
4. Implement `CollectVesselEventsAsync` — vessel creation events
5. Implement `CollectTransactionEventsAsync` — transaction creation events
6. Implement `CollectServiceRequestEventsAsync` — service request creation events
7. Wire `GetUserActivityAsync` in service + interface (STEP 2 — main method)
8. Add controller endpoint (STEP 3)
9. Add validator (STEP 4)
10. Run `dotnet build` — zero errors required
11. Run smoke tests below

---

## STEP 6 — Smoke tests

Replace `{TOKEN}` with `X-Aizen-User-Token: Bearer <identityToken>` and `{USER_ID}` with a real user ID.

```bash
BASE="http://localhost:17001/api/v1/admin-panel"

# All activity, page 1
curl -s "$BASE/admin/users/{USER_ID}/activity" \
  -H "X-Aizen-User-Token: Bearer {TOKEN}" | jq '.body | {total, page, pageSize, count: (.items | length)}'

# Vessel events only
curl -s "$BASE/admin/users/{USER_ID}/activity?category=vessel" \
  -H "X-Aizen-User-Token: Bearer {TOKEN}" | jq '.body.items[].category'

# Identity events only
curl -s "$BASE/admin/users/{USER_ID}/activity?category=identity" \
  -H "X-Aizen-User-Token: Bearer {TOKEN}" | jq '.body.items[] | {type, label, relativeTime}'

# Date filtered
curl -s "$BASE/admin/users/{USER_ID}/activity?dateFrom=2026-01-01&dateTo=2026-06-30" \
  -H "X-Aizen-User-Token: Bearer {TOKEN}" | jq '.body.total'

# Page 2
curl -s "$BASE/admin/users/{USER_ID}/activity?page=2&pageSize=10" \
  -H "X-Aizen-User-Token: Bearer {TOKEN}" | jq '.body | {page, pageSize, total}'

# Invalid category → expect 400
curl -s "$BASE/admin/users/{USER_ID}/activity?category=invalid" \
  -H "X-Aizen-User-Token: Bearer {TOKEN}" | jq '.header.isSuccess'
```

---

## KEY RULES

1. **In-process only.** Never make HTTP calls between modules. Call repositories/services directly.
2. **Graceful degradation.** If a module's DB is unavailable, add a warning and continue. Never throw from `GetUserActivityAsync` unless all modules fail.
3. **Hard cap per module.** Take at most 200 events per module before the merge to avoid memory issues. The sort + paginate happens on the merged list in memory.
4. **Timestamps must be UTC `DateTimeOffset`.** No `DateTime.Now`.
5. **camelCase JSON.** System.Text.Json default serializes PascalCase — ensure `JsonSerializerOptions` has `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` (already set globally in BFF startup).
6. **`type` and `category` fields are snake_case strings** (e.g., `"vessel_registered"`, `"identity"`) — use the `ToEventTypeString` / `ToCategoryString` helpers, never serialize the enum directly.
7. **Do not implement status history** if no `*StatusHistory` table exists. Return only creation events and log a TODO comment.
