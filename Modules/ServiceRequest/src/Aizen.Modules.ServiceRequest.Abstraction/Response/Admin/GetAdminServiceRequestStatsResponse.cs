namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;

[DocumentationInfo("Admin service request stats response",
    "Read-only KPI counts for the admin service-request list, computed module-side over the whole dataset (not page-local).")]
public sealed class GetAdminServiceRequestStatsResponse
{
    /// <summary>All non-deleted service requests.</summary>
    public int TotalRequests { get; set; }

    /// <summary>Service requests currently in progress (Status == InProgress).</summary>
    public int ActiveRepairs { get; set; }

    /// <summary>High-urgency requests (Priority Emergency or Urgent) — mirrors the FE's prior page-local definition, now global.</summary>
    public int CriticalAlerts { get; set; }
}
