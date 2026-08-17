using Aizen.Bff.AdminPanel.Application.Common.Warnings;

namespace Aizen.Bff.AdminPanel.Application.Dashboard.Dto;

[DocumentationInfo("Admin dashboard charts response",
    "Three real chart series for the admin dashboard: service-request volume by status (C2), vessel fleet status (C3), " +
    "and monthly revenue/commission (C1). Each series is best-effort — a downstream failure leaves that series empty " +
    "and appends a warning (never a 500).")]
public sealed class AdminDashboardChartsResponse
{
    /// <summary>C2 — service requests grouped by status.</summary>
    public List<StatusCountDto> ServiceRequestVolume { get; set; } = new();

    /// <summary>C3 — vessels grouped by fleet status.</summary>
    public List<StatusCountDto> VesselFleetStatus { get; set; } = new();

    /// <summary>C1 — last 12 months of revenue + commission (settlement currency), oldest→newest, zero-filled.</summary>
    public List<MonthlyRevenueCommissionDto> Revenue { get; set; } = new();

    public List<AdminBffWarning> Warnings { get; set; } = new();
}
