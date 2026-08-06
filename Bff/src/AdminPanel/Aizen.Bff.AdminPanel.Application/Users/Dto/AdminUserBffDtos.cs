using Aizen.Bff.AdminPanel.Application.Common.Warnings;

namespace Aizen.Bff.AdminPanel.Application.Users.Dto;

// ── User List ─────────────────────────────────────────────────────────────────

[DocumentationInfo("Admin user list BFF response", "Paged user list with UI-ready fields and warnings.")]
public sealed class AdminUserListBffResponse
{
    public UserPageBffDto? Users { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}

[DocumentationInfo("User page BFF DTO", "Pagination wrapper for user list BFF response.")]
public sealed class UserPageBffDto
{
    public int From { get; set; }
    public int Index { get; set; }
    public int Size { get; set; }
    public long Count { get; set; }
    public int Pages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
    public List<AdminUserListItemBffDto> Items { get; set; } = new();
}

[DocumentationInfo("Admin user list item BFF DTO", "Single user row for the user management list.")]
public sealed class AdminUserListItemBffDto
{
    public string Id { get; set; } = null!;            // profile ID as string
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Email { get; set; }                 // null – not exposed in profile list
    public string? Phone { get; set; }                 // null – not tracked in profile
    public string Role { get; set; } = null!;          // mapped from RoleContext
    public string Status { get; set; } = null!;        // mapped from ApprovalStatus
    public string IdentityType { get; set; } = null!;  // RoleContext (Organizer/Participant/Venue)
    public int VesselCount { get; set; }               // 0 – TODO: needs batch vessel-count endpoint
    public string? LastLoginAt { get; set; }            // null – not tracked locally
    public string CreatedAt { get; set; } = null!;
    public string AvatarInitials { get; set; } = null!;
}

// ── KPI ──────────────────────────────────────────────────────────────────────

[DocumentationInfo("Admin user KPI BFF response", "Four aggregate counters for the user management KPI bar.")]
public sealed class AdminUserKpiBffResponse
{
    public int TotalUsers { get; set; }
    public int ActiveToday { get; set; }
    public int PendingVerification { get; set; }
    public int Suspended { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}

// ── User Detail ───────────────────────────────────────────────────────────────

[DocumentationInfo("Admin user detail BFF response", "Full user profile for the detail page.")]
public sealed class AdminUserDetailBffResponse
{
    public AdminUserDetailBffDto? User { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}

[DocumentationInfo("Admin user detail BFF DTO", "Detailed user information for the admin detail view.")]
public sealed class AdminUserDetailBffDto
{
    public string Id { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Email { get; set; }
    public bool EmailVerified { get; set; }
    public string? Phone { get; set; }
    public string Role { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string IdentityType { get; set; } = null!;
    public string? Bio { get; set; }
    public string AvatarInitials { get; set; } = null!;
    public string? LastLoginAt { get; set; }
    public string CreatedAt { get; set; } = null!;
    public int VesselCount { get; set; }
    public int TotalTransactions { get; set; }
    public int TotalServiceRequests { get; set; }
}

// ── Quick Panel ───────────────────────────────────────────────────────────────

[DocumentationInfo("Admin user quick BFF response", "Sidebar quick-view panel data for a single user.")]
public sealed class AdminUserQuickBffResponse
{
    public AdminUserQuickBffDto? User { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}

[DocumentationInfo("Admin user quick BFF DTO", "Compact user summary for the quick-view panel.")]
public sealed class AdminUserQuickBffDto
{
    public string Id { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Role { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string IdentityType { get; set; } = null!;
    public string AvatarInitials { get; set; } = null!;
    public string? LastLoginAt { get; set; }
    public string CreatedAt { get; set; } = null!;
    public int VesselCount { get; set; }
    public List<AdminUserVesselSummaryBffDto> Vessels { get; set; } = new();
    public List<AdminUserActivityItemBffDto> RecentActivity { get; set; } = new();
}

public sealed class AdminUserVesselSummaryBffDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Type { get; set; }
}

public sealed class AdminUserActivityItemBffDto
{
    public string Id { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public string Label { get; set; } = null!;
    public string Timestamp { get; set; } = null!;
    public string RelativeTime { get; set; } = null!;
}

// ── Vessels Tab ───────────────────────────────────────────────────────────────

[DocumentationInfo("Admin user vessels BFF response", "Vessel list for a specific user's vessels tab.")]
public sealed class AdminUserVesselsBffResponse
{
    public List<AdminUserVesselRowBffDto> Items { get; set; } = new();
    public List<AdminBffWarning> Warnings { get; set; } = new();
}

public sealed class AdminUserVesselRowBffDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Type { get; set; }
    public string? FlagCountry { get; set; }
    public string? RegistrationNo { get; set; }
    public string Status { get; set; } = null!;
    public string RegisteredAt { get; set; } = null!;
}

// ── Activity Tab ──────────────────────────────────────────────────────────────

[DocumentationInfo("Admin user activity BFF response", "Paginated, filterable activity timeline events for a specific user.")]
public sealed class AdminUserActivityBffResponse
{
    public List<AdminUserActivityEventBffDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public List<AdminBffWarning> Warnings { get; set; } = new();
}

public sealed class AdminUserActivityEventBffDto
{
    public string Id { get; set; } = null!;
    public string Type { get; set; } = null!;          // snake_case event type
    public string Category { get; set; } = null!;      // vessel | service | identity | system
    public string Icon { get; set; } = null!;
    public string Label { get; set; } = null!;
    public string? Description { get; set; }
    public string Timestamp { get; set; } = null!;     // ISO 8601 UTC
    public string RelativeTime { get; set; } = null!;
    public AdminUserActivityMetaBffDto? Meta { get; set; }
}

public sealed class AdminUserActivityMetaBffDto
{
    public string? EntityId { get; set; }
    public string? EntityName { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
}
