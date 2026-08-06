namespace Aizen.Bff.AdminPanel.Application.Users;

internal static class AdminUserBffHelpers
{
    internal static string ComputeAvatarInitials(string? firstName, string? lastName)
    {
        var f = firstName?.Length > 0 ? firstName[0].ToString().ToUpperInvariant() : string.Empty;
        var l = lastName?.Length > 0 ? lastName[0].ToString().ToUpperInvariant() : string.Empty;
        return $"{f}{l}";
    }

    internal static string ComputeRelativeTime(DateTime eventTime)
    {
        var diff = DateTime.UtcNow - eventTime;
        if (diff.TotalMinutes < 1) return "Just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} minutes ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hours ago";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} days ago";
        if (diff.TotalDays < 30) return $"{(int)(diff.TotalDays / 7)} weeks ago";
        if (diff.TotalDays < 365) return $"{(int)(diff.TotalDays / 30)} months ago";
        return $"{(int)(diff.TotalDays / 365)} years ago";
    }

    /// <summary>Maps ApprovalStatus from Identity module to a UI-friendly status string.</summary>
    internal static string MapStatus(string? approvalStatus, string? profileStatus)
    {
        // ApprovalStatus: Pending | Approved | Rejected
        // Profile Status: Active | Inactive | ...
        return approvalStatus switch
        {
            "Pending" => "Pending",
            "Rejected" => "Deactivated",
            "Approved" => profileStatus switch
            {
                "Inactive" => "Suspended",
                _ => "Active"
            },
            _ => profileStatus ?? "Unknown"
        };
    }

    /// <summary>Maps RoleContext to a UI role label.</summary>
    internal static string MapRole(string? roleContext) => roleContext switch
    {
        "Organizer" => "Organizer",
        "Participant" => "Participant",
        "Venue" => "VenueManager",
        _ => roleContext ?? "Unknown"
    };
}
