using Aizen.Bff.AdminPanel.Application.Common;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;

[DocumentationInfo("ServiceRequest stats BFF DTO",
    "KPI stats for the admin SR list. totalRequests/activeRepairs/criticalAlerts are real module counts; repairVelocity/recentAlerts are deferred (empty) — the FE renders its localized empty-state for those.")]
public sealed class ServiceRequestStatsBffDto
{
    public int TotalRequests { get; set; }
    public int ActiveRepairs { get; set; }
    public int CriticalAlerts { get; set; }

    /// <summary>Deferred (empty). See REPORT_BE_QA4 §C.</summary>
    public List<ServiceRequestVelocityPointBffDto> RepairVelocity { get; set; } = new();

    /// <summary>Deferred (empty). See REPORT_BE_QA4 §C.</summary>
    public List<ServiceRequestAlertBffDto> RecentAlerts { get; set; } = new();
}

public sealed class ServiceRequestVelocityPointBffDto
{
    public string Day { get; set; } = default!;
    public int Count { get; set; }
}

public sealed class ServiceRequestAlertBffDto
{
    public string Id { get; set; } = default!;
    public string Severity { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Timestamp { get; set; } = default!;
}
